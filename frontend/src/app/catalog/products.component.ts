import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../core/api.service';
import { Product } from '../models';

/**
 * Lists the rice catalog and lets a customer add items to their cart.
 * Demonstrates the frontend -> gateway -> Catalog/Cart service path.
 */
@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Our Rice Selection</h2>
    <div class="grid" *ngIf="products.length; else loading">
      <div class="card" *ngFor="let p of products">
        <h3>{{ p.name }}</h3>
        <p class="brand">{{ p.brand }} &middot; {{ p.weightKg }}kg</p>
        <p>{{ p.description }}</p>
        <p class="price">{{ p.price | currency }}</p>
        <button (click)="addToCart(p)">Add to cart</button>
      </div>
    </div>
    <ng-template #loading><p>Loading rice...</p></ng-template>
  `,
})
export class ProductsComponent implements OnInit {
  private api = inject(ApiService);
  products: Product[] = [];

  // In a real app this comes from the logged-in user (Identity token).
  customerId = 'demo-customer';

  ngOnInit(): void {
    this.api.getProducts().subscribe((p) => (this.products = p));
  }

  addToCart(p: Product): void {
    this.api
      .addToCart(this.customerId, {
        productId: p.id,
        productName: p.name,
        unitPrice: p.price,
        quantity: 1,
      })
      .subscribe();
  }
}
