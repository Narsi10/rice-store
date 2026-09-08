import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Product, ShoppingCart, CartItem, AuthResponse,
  PlaceOrderRequest, Order,
} from '../models';

/**
 * Single typed gateway client. Every call goes to the API Gateway
 * (http://localhost:8000), which routes to the right microservice.
 * The browser never talks to a service directly.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly base = 'http://localhost:8000';

  constructor(private http: HttpClient) {}

  // --- Identity service (via /api/auth) ---
  register(email: string, password: string, fullName: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/api/auth/register`, { email, password, fullName });
  }
  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/api/auth/login`, { email, password });
  }

  // --- Catalog service (via /api/products) ---
  getProducts(search?: string): Observable<Product[]> {
    const q = search ? `?search=${encodeURIComponent(search)}` : '';
    return this.http.get<Product[]>(`${this.base}/api/products${q}`);
  }
  getProduct(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.base}/api/products/${id}`);
  }

  // --- Cart service (via /api/cart) ---
  getCart(customerId: string): Observable<ShoppingCart> {
    return this.http.get<ShoppingCart>(`${this.base}/api/cart/${customerId}`);
  }
  addToCart(customerId: string, item: CartItem): Observable<ShoppingCart> {
    return this.http.post<ShoppingCart>(`${this.base}/api/cart/${customerId}/items`, item);
  }
  removeFromCart(customerId: string, productId: string): Observable<ShoppingCart> {
    return this.http.delete<ShoppingCart>(`${this.base}/api/cart/${customerId}/items/${productId}`);
  }

  // --- Order service (via /api/orders) ---
  placeOrder(request: PlaceOrderRequest): Observable<Order> {
    return this.http.post<Order>(`${this.base}/api/orders`, request);
  }
  getOrder(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.base}/api/orders/${id}`);
  }
  getCustomerOrders(customerId: string): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.base}/api/orders/customer/${customerId}`);
  }
}
