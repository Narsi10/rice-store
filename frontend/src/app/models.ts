// TypeScript models mirroring the backend C# DTOs.

export enum RiceType {
  Basmati = 0,
  BrownRice = 1,
  Jasmine = 2,
  SonaMasoori = 3,
  Parboiled = 4,
  Idli = 5,
  Sticky = 6,
  Other = 99,
}

export interface Product {
  id: string;
  name: string;
  description: string;
  riceType: RiceType;
  brand: string;
  price: number;
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

export interface AuthResponse {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  token: string;
}

export interface PlaceOrderRequest {
  customerId: string;
  customerEmail: string;
  items: { productId: string; productName: string; quantity: number; unitPrice: number }[];
}

export interface Order {
  id: string;
  customerId: string;
  customerEmail: string;
  status: number;
  totalAmount: number;
  items: CartItem[];
}
