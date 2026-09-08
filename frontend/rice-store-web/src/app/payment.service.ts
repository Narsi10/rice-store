import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, delay, firstValueFrom, of } from 'rxjs';
import { PaymentDetails, PaymentResult } from './models';
import { runtimeConfig } from './app-config';

// Razorpay checkout is loaded globally via index.html.
declare const Razorpay: any;

interface RazorpayConfigResponse { enabled: boolean; keyId: string; }
interface CreateOrderResponse { keyId: string; orderId: string; amount: number; currency: string; }

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);
  private get gateway() { return runtimeConfig.gatewayUrl; }

  /** Checks whether real Razorpay is configured on the backend. */
  async isRazorpayEnabled(): Promise<boolean> {
    const cfg = await firstValueFrom(
      this.http
        .get<RazorpayConfigResponse>(`${this.gateway}/api/payments/razorpay/config`)
        .pipe(catchError(() => of({ enabled: false, keyId: '' }))),
    );
    return cfg.enabled;
  }

  /**
   * Real Razorpay flow: create an order on the backend, open Razorpay checkout
   * (cards / UPI / netbanking), then verify the signature on the backend.
   * Resolves to a PaymentResult. Rejects/returns failure on cancel or error.
   */
  async payWithRazorpay(
    orderId: string,
    amountInr: number,
    customerEmail: string,
  ): Promise<PaymentResult> {
    // 1. Create the Razorpay order via backend.
    const order = await firstValueFrom(
      this.http
        .post<CreateOrderResponse>(`${this.gateway}/api/payments/razorpay/create-order`, {
          amountInr,
          receipt: orderId,
        })
        .pipe(catchError(() => of(null))),
    );
    if (!order) {
      return { success: false, method: 'card', maskedInstrument: '', error: 'Could not start payment.' };
    }

    // 2. Open Razorpay checkout and wait for the result.
    return new Promise<PaymentResult>((resolve) => {
      const options = {
        key: order.keyId,
        amount: order.amount,
        currency: order.currency,
        name: 'Rice Store',
        description: 'Order payment',
        order_id: order.orderId,
        prefill: { email: customerEmail },
        theme: { color: '#b5822e' },
        handler: async (resp: any) => {
          // 3. Verify signature on the backend.
          const verified = await firstValueFrom(
            this.http
              .post<{ verified: boolean; transactionId: string }>(
                `${this.gateway}/api/payments/razorpay/verify`,
                {
                  razorpayOrderId: resp.razorpay_order_id,
                  razorpayPaymentId: resp.razorpay_payment_id,
                  razorpaySignature: resp.razorpay_signature,
                  orderId,
                  amountInr,
                },
              )
              .pipe(catchError(() => of({ verified: false, transactionId: '' }))),
          );

          resolve(
            verified.verified
              ? { success: true, method: 'card', maskedInstrument: resp.razorpay_payment_id, transactionId: verified.transactionId }
              : { success: false, method: 'card', maskedInstrument: '', error: 'Payment verification failed.' },
          );
        },
        modal: {
          ondismiss: () =>
            resolve({ success: false, method: 'card', maskedInstrument: '', error: 'Payment cancelled.' }),
        },
      };
      new Razorpay(options).open();
    });
  }

  // ----- Simulated fallback (used when Razorpay isn't configured) -----

  validate(d: PaymentDetails): string | null {
    if (d.method === 'card') {
      const digits = (d.cardNumber ?? '').replace(/\s+/g, '');
      if (!/^\d{16}$/.test(digits)) return 'Enter a valid 16-digit card number.';
      if (!this.luhnValid(digits)) return 'That card number looks invalid.';
      if (!d.cardName?.trim()) return 'Enter the name on the card.';
      if (!/^\d{2}\/\d{2}$/.test(d.cardExpiry ?? '')) return 'Expiry must be MM/YY.';
      if (this.isExpired(d.cardExpiry!)) return 'This card has expired.';
      if (!/^\d{3,4}$/.test(d.cardCvv ?? '')) return 'CVV must be 3 or 4 digits.';
    } else if (d.method === 'upi') {
      if (!/^[\w.\-]{2,}@[a-zA-Z]{2,}$/.test(d.upiId ?? ''))
        return 'Enter a valid UPI ID (e.g. name@bank).';
    }
    return null;
  }

  process(d: PaymentDetails): Observable<PaymentResult> {
    const masked =
      d.method === 'card'
        ? `**** ${(d.cardNumber ?? '').replace(/\s+/g, '').slice(-4)}`
        : (d.upiId ?? '');
    return of<PaymentResult>({
      success: true,
      transactionId: `txn_${crypto.randomUUID().replace(/-/g, '').slice(0, 16)}`,
      method: d.method,
      maskedInstrument: masked,
    }).pipe(delay(1200));
  }

  private luhnValid(num: string): boolean {
    let sum = 0, alt = false;
    for (let i = num.length - 1; i >= 0; i--) {
      let n = parseInt(num[i], 10);
      if (alt) { n *= 2; if (n > 9) n -= 9; }
      sum += n; alt = !alt;
    }
    return sum % 10 === 0;
  }

  private isExpired(mmYy: string): boolean {
    const [mm, yy] = mmYy.split('/').map((s) => parseInt(s, 10));
    if (mm < 1 || mm > 12) return true;
    const expiry = new Date(2000 + yy, mm, 0, 23, 59, 59);
    return expiry.getTime() < Date.now();
  }
}
