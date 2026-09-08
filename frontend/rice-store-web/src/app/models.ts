// TypeScript models mirroring the backend C# DTOs.

export enum RiceType {
  HMT = 0,
  SonaMasoori = 1,
  Basmati = 2,
  SinglePolish = 3,
  JaiShreeRam = 4,
  Other = 99,
}

export const RiceTypeLabel: Record<number, string> = {
  0: 'HMT',
  1: 'Sona Masoori',
  2: 'Basmati',
  3: 'Single Polish',
  4: 'Jai Shree Ram',
  99: 'Other',
};

export interface Product {
  id: string;
  name: string;
  description: string;
  riceType: RiceType;
  brand: string;
  price: number;        // selling price (INR)
  costPrice: number;    // buying/cost price (INR) — for profit tracking
  stock: number;        // units available
  weightKg: number;
  imageUrl: string;
  isActive: boolean;
}

export interface CartItem {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface ShoppingCart {
  customerId: string;
  items: CartItem[];
  total: number;
}

export enum OrderStatus {
  Pending = 0,
  StockReserved = 1,
  Paid = 2,
  Confirmed = 3,
  Cancelled = 4,
  Approved = 5,
  Rejected = 6,
}

export const OrderStatusLabel: Record<number, string> = {
  0: 'Pending',
  1: 'Stock Reserved',
  2: 'Paid',
  3: 'Awaiting Approval',
  4: 'Cancelled',
  5: 'Approved',
  6: 'Rejected',
};

export interface Order {
  id: string;
  createdOnUtc: string;
  customerId: string;
  customerEmail: string;
  status: OrderStatus;
  totalAmount: number;
  items: CartItem[];
  reviewedBy?: string | null;
  reviewedOnUtc?: string | null;
  reviewNote?: string | null;
  // Payment
  paymentMethod?: string | null;
  paymentInstrument?: string | null;
  transactionId?: string | null;
}

export interface AuthUser {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  token: string;
}

export interface AppNotification {
  id: string;
  customerId: string;
  message: string;
  orderId: string;
  type: 'approved' | 'rejected' | 'info';
  createdOnUtc: string;
  read: boolean;
}

export type PaymentMethod = 'card' | 'upi';

export interface PaymentDetails {
  method: PaymentMethod;
  // Card
  cardNumber?: string;
  cardName?: string;
  cardExpiry?: string;
  cardCvv?: string;
  cardType?: 'credit' | 'debit';
  // UPI
  upiId?: string;
}

export interface PaymentResult {
  success: boolean;
  transactionId?: string;
  method: PaymentMethod;
  maskedInstrument: string; // e.g. "**** 4242" or "name@upi"
  error?: string;
}
