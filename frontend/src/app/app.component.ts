import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Bell, Boxes, ChevronDown, CircleDollarSign, ClipboardList, FileBarChart, Home, LogOut, Menu, Package, Search, Settings, ShoppingCart, Store, Truck, Users, X, LucideAngularModule } from 'lucide-angular';

interface Product { productoId?: number; nombre?: string; codigo?: string; precioVenta?: number; stock?: number; activo?: boolean; }
interface Inventory { productoId?: number; nombreProducto?: string; stock?: number; stockMinimo?: number; }

@Component({ selector: 'app-root', standalone: true, imports: [CommonModule, FormsModule, HttpClientModule, LucideAngularModule], templateUrl: './app.component.html' })
export class AppComponent {
  private http = inject(HttpClient);
  readonly icons = { Bell, Boxes, ChevronDown, CircleDollarSign, ClipboardList, FileBarChart, Home, LogOut, Menu, Package, Search, Settings, ShoppingCart, Store, Truck, Users, X };
  readonly api = 'http://localhost:5008/api';
  loginMode = !sessionStorage.getItem('webwave_token');
  loading = false; error = '';
  credentials = { email: '', password: '' };
  active = 'Resumen'; search = ''; sidebarOpen = false;
  products: Product[] = []; inventory: Inventory[] = [];
  nav = [
    { label: 'Resumen', icon: 'Home' }, { label: 'Inventario', icon: 'Boxes' }, { label: 'Productos', icon: 'Package' },
    { label: 'Ventas', icon: 'ShoppingCart' }, { label: 'Compras', icon: 'Truck' }, { label: 'Clientes', icon: 'Users' }
  ];
  stats = [
    { label: 'Ventas del mes', value: '$ 48,280.00', trend: '+12.8%', icon: 'CircleDollarSign', tone: 'green' },
    { label: 'Productos activos', value: '1,284', trend: '+4.2%', icon: 'Package', tone: 'blue' },
    { label: 'Stock bajo', value: '18', trend: 'Requieren atención', icon: 'Boxes', tone: 'orange' },
    { label: 'Clientes registrados', value: '842', trend: '+8.1%', icon: 'Users', tone: 'purple' }
  ];
  recentSales = [
    { id: '#V-1048', customer: 'Distribuidora El Sol', date: 'Hoy, 10:42 AM', amount: '$ 1,240.00', status: 'Completada' },
    { id: '#V-1047', customer: 'Mercado Central', date: 'Hoy, 09:18 AM', amount: '$ 840.50', status: 'Completada' },
    { id: '#V-1046', customer: 'Laura Hernández', date: 'Ayer, 04:35 PM', amount: '$ 325.00', status: 'Pendiente' },
    { id: '#V-1045', customer: 'Comercial López', date: 'Ayer, 01:12 PM', amount: '$ 2,180.00', status: 'Completada' }
  ];

  login() {
    this.loading = true; this.error = '';
    this.http.post<any>(`${this.api}/Acceso/Login`, this.credentials).subscribe({
      next: (response) => { const token = response.token ?? response.accessToken ?? response.jwt; if (token) sessionStorage.setItem('webwave_token', token); this.loginMode = false; this.loading = false; this.loadData(); },
      error: () => { this.error = 'No pudimos iniciar sesión. Verifica tus credenciales y que la API esté activa.'; this.loading = false; }
    });
  }
  logout() { sessionStorage.removeItem('webwave_token'); this.loginMode = true; }
  setActive(label: string) { this.active = label; this.sidebarOpen = false; if (label === 'Productos') this.loadProducts(); if (label === 'Inventario') this.loadInventory(); }
  loadData() { this.loadProducts(); this.loadInventory(); }
  loadProducts() { this.http.get<Product[]>(`${this.api}/Productos`).subscribe({ next: d => this.products = Array.isArray(d) ? d : [], error: () => this.products = [] }); }
  loadInventory() { this.http.get<Inventory[]>(`${this.api}/Inventario`).subscribe({ next: d => this.inventory = Array.isArray(d) ? d : [], error: () => this.inventory = [] }); }
  iconFor(name: string) { return this.icons[name as keyof typeof this.icons]; }
  get filteredProducts() { return this.products.filter(p => `${p.nombre} ${p.codigo}`.toLowerCase().includes(this.search.toLowerCase())); }
}
