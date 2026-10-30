import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  Bell, Boxes, ChevronDown, CircleDollarSign, ClipboardList,
  FileBarChart, Home, LogOut, Menu, Package, Search,
  Settings, ShoppingCart, Store, Truck, Users, X, FileText, CheckCircle2,
  AlertTriangle, Plus, Lock, Printer, Trash2, Check, Shield, RotateCcw,
  Pencil, History, Ban, Layers, LucideAngularModule
} from 'lucide-angular';

interface Product {
  productoId?: number;
  nombre?: string;
  codigo?: string;
  codigoBarras?: string;
  precioVenta?: number;
  precio?: number;
  stock?: number;
  tipoISV?: string;
  activo?: boolean;
}

interface Inventory {
  inventarioId?: number;
  productoId?: number;
  producto?: string;
  nombreProducto?: string;
  stock?: number;
  stockMinimo?: number;
}

interface Cliente {
  clienteId: number;
  nombre: string;
  telefono?: string;
  email?: string;
  rtn?: string;
  direccion?: string;
  tipoCliente?: string;
  activo?: boolean;
}

interface Proveedor {
  proveedorId: number;
  nombre: string;
  contacto?: string;
  telefono?: string;
  email?: string;
  direccion?: string;
  rtn?: string;
  activo?: boolean;
}

interface Compra {
  compraId: number;
  proveedorId: number;
  proveedor: string;
  fechaCompra: string;
  total: number;
  estadoCompra: string;
  detalles: any[];
}

interface MovimientoKardex {
  movimientoId: number;
  inventarioId: number;
  productoId: number;
  producto: string;
  cantidad: number;
  tipoMovimiento: string;
  fechaMovimiento: string;
}

interface EstadoCaja {
  id?: number;
  estado: string; // 'Abierto' | 'Cerrado'
  fechaApertura?: string;
  montoApertura?: number;
  totalVentasEfectivo?: number;
  totalVentasTarjeta?: number;
  totalVentasOtros?: number;
}

interface CierreCajaItem {
  id: number;
  sucursalId: number;
  usuarioId: number;
  fechaApertura: string;
  fechaCierre?: string;
  montoApertura: number;
  montoEfectivoFinal?: number;
  totalVentasEfectivo: number;
  totalVentasTarjeta: number;
  diferencia?: number;
  estado: string;
}

interface UsuarioItem {
  usuarioId: number;
  nombre: string;
  email: string;
  perfilId: number;
  perfil: string;
}

interface PerfilItem {
  perfilId: number;
  nombre: string;
}

interface VentaItem {
  ventaId: number;
  clienteId: number;
  cliente: string;
  clienteRtn?: string;
  fechaVenta: string;
  total: number;
  estadoVenta: string;
  numeroFactura?: string;
  cai?: string;
  isv15?: number;
  isv18?: number;
  montoExento?: number;
  montoGravado15?: number;
  fechaFactura?: string;
  detalles: any[];
  pagos: any[];
}

interface SARConfig {
  id: number;
  sucursalId: number;
  rtn: string;
  cai: string;
  rangoDesde: string;
  rangoHasta: string;
  fechaLimiteEmision: string;
  correlativoActual: number;
  siguienteFactura: string;
  facturasDisponibles: number;
  vigente: boolean;
}

export interface FacturaItemRow {
  productoId: number;
  nombre: string;
  cantidad: number;
  precio: number;
  descuento: number;
  tipoISV: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './app.component.html'
})
export class AppComponent {
  private http = inject(HttpClient);

  readonly icons = {
    Bell, Boxes, ChevronDown, CircleDollarSign, ClipboardList,
    FileBarChart, Home, LogOut, Menu, Package, Search,
    Settings, ShoppingCart, Store, Truck, Users, X, FileText, CheckCircle2,
    AlertTriangle, Plus, Lock, Printer, Trash2, Check, Shield, RotateCcw,
    Pencil, History, Ban, Layers
  };

  readonly api = 'http://localhost:5008/api';
  loginMode = !sessionStorage.getItem('webwave_token');
  loading = false;
  error = '';
  credentials = { email: '', password: '' };
  active = 'Resumen';
  search = '';
  sidebarOpen = false;

  currentUser = {
    name: sessionStorage.getItem('webwave_user_name') || 'Angel Guillen',
    role: sessionStorage.getItem('webwave_user_role') || 'Administrador',
    initials: sessionStorage.getItem('webwave_user_initials') || 'AG'
  };

  get isAdmin(): boolean {
    const role = (this.currentUser.role || '').toLowerCase();
    return role.includes('admin') || role === 'administrador';
  }

  // Colecciones de Datos del API
  products: Product[] = [];
  inventory: Inventory[] = [];
  ventas: VentaItem[] = [];
  clientes: Cliente[] = [];
  proveedores: Proveedor[] = [];
  compras: Compra[] = [];
  movimientosKardex: MovimientoKardex[] = [];
  estadoCaja: EstadoCaja = { estado: 'Cerrado' };
  historialCaja: CierreCajaItem[] = [];
  usuarios: UsuarioItem[] = [];
  perfiles: PerfilItem[] = [];

  // Pestañas secundarias en Inventario
  subTabInventario: 'stock' | 'kardex' = 'stock';

  // Configuración SAR
  sarConfig: SARConfig | null = null;
  showSARConfigModal = false;
  showFacturaModal = false;
  showNuevoProductoModal = false;
  showNuevoClienteModal = false;
  showMovimientoInventarioModal = false;
  facturaSeleccionada: any = null;

  // Modales de Compras y Proveedores
  showNuevaCompraModal = false;
  guardandoCompra = false;
  nuevaCompra: any = {
    proveedorId: 0,
    items: [{ productoId: 0, cantidad: 1, precioUnitario: 0 }]
  };

  showProveedorModal = false;
  guardandoProveedor = false;
  proveedorEditandoId: number | null = null;
  proveedorForm: any = {
    nombre: '',
    contacto: '',
    telefono: '',
    email: '',
    direccion: '',
    rtn: ''
  };

  // Modales de Caja
  showAbrirCajaModal = false;
  montoAperturaInput = 1000;
  showCerrarCajaModal = false;
  montoCierreInput = 0;

  // Modales de Usuarios
  showNuevoUsuarioModal = false;
  guardandoUsuario = false;
  nuevoUsuarioForm: any = {
    nombre: '',
    email: '',
    password: '',
    perfilId: 1
  };

  // Modales de Edición de Productos y Clientes
  showEditarProductoModal = false;
  guardandoEditarProducto = false;
  productoEditando: any = {
    productoId: 0,
    nombre: '',
    descripcion: '',
    codigoBarras: '',
    precio: 0,
    tipoISV: 'ISV15'
  };

  showEditarClienteModal = false;
  guardandoEditarCliente = false;
  clienteEditando: any = {
    clienteId: 0,
    nombre: '',
    rtn: '',
    telefono: '',
    email: '',
    direccion: '',
    tipoCliente: 'Natural'
  };

  // Modelos de registro
  guardandoProducto = false;
  nuevoProducto = {
    nombre: '',
    descripcion: '',
    codigoBarras: '',
    precio: 0,
    tipoISV: 'ISV15',
    tieneNumeroSerie: false,
    tieneLote: false,
    stockInicial: 25
  };

  nuevoCliente = {
    nombre: '',
    rtn: '',
    telefono: '',
    email: '',
    direccion: 'Tegucigalpa, Honduras',
    tipoCliente: 'Natural'
  };

  movimientoInventario = {
    productoId: 0,
    tipoMovimiento: 'ENTRADA',
    cantidad: 10
  };

  nuevaVenta = {
    clienteId: 1,
    productoId: 1,
    cantidad: 1,
    metodoPagoId: 1,
    concretarVenta: true,
    emitirFactura: true
  };

  sarForm = {
    sucursalId: 1,
    rtn: '08011990123456',
    cai: '3B8E12-4C67D8-9A10B2-3C4D5E-6F7A8B-9C',
    rangoDesde: '000-001-01-00000001',
    rangoHasta: '000-001-01-00005000',
    fechaLimiteEmision: '2027-12-31',
    resetearCorrelativo: false
  };

  // Facturación en vivo
  facturaForm = {
    clienteId: 1,
    fecha: new Date().toISOString().split('T')[0],
    metodoPagoId: 1,
    moneda: 'Lempiras (HNL)',
    notas: '',
    notasPie: 'Gracias por su compra en Sistema Vision Tech. Garantía de 6 meses en monturas.',
    mostrarNotas: false,
    mostrarNotasPie: false,
    items: [
      {
        productoId: 1,
        nombre: 'Lente Antirreflejo HD',
        cantidad: 1,
        precio: 1250.00,
        descuento: 0,
        tipoISV: 'ISV15'
      }
    ] as FacturaItemRow[]
  };

  facturaEmitida = false;
  generandoFactura = false;
  facturaEmitidaData: any = null;

  nav = [
    { label: 'Resumen', icon: 'Home' },
    { label: 'Facturación', icon: 'FileText' },
    { label: 'Ventas', icon: 'ShoppingCart' },
    { label: 'Compras', icon: 'Truck' },
    { label: 'Inventario', icon: 'Boxes' },
    { label: 'Productos', icon: 'Package' },
    { label: 'Clientes', icon: 'Users' },
    { label: 'Proveedores', icon: 'Store' },
    { label: 'Caja', icon: 'CircleDollarSign' },
    { label: 'Usuarios', icon: 'Shield' }
  ];

  stats = [
    { label: 'Ventas del mes', value: 'L. 0.00', trend: 'Calculado en vivo', icon: 'CircleDollarSign', tone: 'green' },
    { label: 'Productos activos', value: '0', trend: 'Catálogo sincronizado', icon: 'Package', tone: 'blue' },
    { label: 'Stock bajo', value: '0', trend: 'Requieren atención', icon: 'Boxes', tone: 'orange' },
    { label: 'Estado de Caja', value: 'Cerrada', trend: 'Turno actual', icon: 'CircleDollarSign', tone: 'purple' }
  ];

  constructor() {
    if (!this.loginMode) {
      this.loadData();
    }
  }

  // Encabezados HTTP con Token JWT
  private getHeaders(): { headers: HttpHeaders } {
    const token = sessionStorage.getItem('webwave_token') || '';
    let headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    return { headers };
  }

  private handleAuthError(err: any): boolean {
    if (err.status === 401) {
      alert('Tu sesión ha expirado o el token de seguridad no es válido. Por favor vuelve a iniciar sesión.');
      this.logout();
      return true;
    }
    return false;
  }

  login() {
    this.loading = true;
    this.error = '';

    const payload = {
      email: this.credentials.email.trim(),
      password: this.credentials.password,
      contraseña: this.credentials.password
    };

    this.http.post<any>(`${this.api}/Acceso/Login`, payload).subscribe({
      next: (response) => {
        const token = response.token ?? response.accessToken ?? response.jwt;
        if (token) {
          sessionStorage.setItem('webwave_token', token);
        }

        if (response.nombre) {
          sessionStorage.setItem('webwave_user_name', response.nombre);
          sessionStorage.setItem('webwave_user_role', response.perfil || 'Administrador');
          const initials = response.nombre
            .split(' ')
            .map((n: string) => n[0])
            .join('')
            .substring(0, 2)
            .toUpperCase();
          sessionStorage.setItem('webwave_user_initials', initials);
          this.currentUser = {
            name: response.nombre,
            role: response.perfil || 'Administrador',
            initials: initials || 'AG'
          };
        }

        this.loginMode = false;
        this.loading = false;
        this.loadData();
      },
      error: (err) => {
        const msg = err.error?.mensaje || err.error?.message;
        this.error = msg || 'No pudimos iniciar sesión. Verifica tus credenciales y que el API esté activa.';
        this.loading = false;
      }
    });
  }

  logout() {
    sessionStorage.removeItem('webwave_token');
    sessionStorage.removeItem('webwave_user_name');
    sessionStorage.removeItem('webwave_user_role');
    sessionStorage.removeItem('webwave_user_initials');
    this.loginMode = true;
    this.products = [];
    this.inventory = [];
    this.ventas = [];
    this.compras = [];
    this.proveedores = [];
  }

  setActive(label: string) {
    this.active = label;
    this.sidebarOpen = false;
    if (label === 'Resumen') this.loadData();
    if (label === 'Facturación') {
      this.loadClientes();
      this.loadProducts();
      this.loadSARConfig();
    }
    if (label === 'Ventas') {
      this.loadVentas();
      this.loadClientes();
      this.loadProducts();
      this.loadSARConfig();
    }
    if (label === 'Compras') {
      this.loadCompras();
      this.loadProveedores();
      this.loadProducts();
    }
    if (label === 'Inventario') {
      this.loadInventory();
      this.loadMovimientos();
    }
    if (label === 'Productos') this.loadProducts();
    if (label === 'Clientes') this.loadClientes();
    if (label === 'Proveedores') this.loadProveedores();
    if (label === 'Caja') {
      this.loadCajaEstado();
      this.loadCajaHistorial();
    }
    if (label === 'Usuarios') {
      this.loadUsuarios();
      this.loadPerfiles();
    }
  }

  loadData() {
    this.loadProducts();
    this.loadInventory();
    this.loadVentas();
    this.loadClientes();
    this.loadSARConfig();
    this.loadCompras();
    this.loadProveedores();
    this.loadCajaEstado();
  }

  loadProducts() {
    this.http.get<any[]>(`${this.api}/Productos`, this.getHeaders()).subscribe({
      next: (data) => {
        const items = Array.isArray(data) ? data : [];
        this.products = items.map((p: any) => ({
          productoId: p.productoId,
          nombre: p.nombre,
          codigo: p.codigoBarras || p.codigo || '—',
          codigoBarras: p.codigoBarras,
          precioVenta: p.precio ?? p.precioVenta ?? 0,
          precio: p.precio ?? 0,
          stock: p.stock ?? 0,
          tipoISV: p.tipoISV || 'ISV15',
          activo: p.activo ?? true
        }));
        this.stats[1].value = this.products.length.toString();

        if (this.products.length > 0) {
          if (!this.nuevaVenta.productoId || !this.products.some(p => p.productoId === this.nuevaVenta.productoId)) {
            this.nuevaVenta.productoId = this.products[0].productoId!;
          }
          if (this.facturaForm.items.length > 0 && !this.facturaForm.items[0].nombre) {
            this.facturaForm.items[0].productoId = this.products[0].productoId!;
            this.onProductoCambio(this.facturaForm.items[0]);
          }
        }
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.products = [];
        }
      }
    });
  }

  loadInventory() {
    this.http.get<any[]>(`${this.api}/Inventario`, this.getHeaders()).subscribe({
      next: (data) => {
        const items = Array.isArray(data) ? data : [];
        this.inventory = items.map((i: any) => ({
          inventarioId: i.inventarioId,
          productoId: i.productoId,
          nombreProducto: i.producto || i.nombreProducto || `Producto #${i.productoId}`,
          stock: i.cantidad ?? i.stock ?? 0,
          stockMinimo: i.stockMinimo ?? 10
        }));

        const lowStock = this.inventory.filter(i => (i.stock ?? 0) <= (i.stockMinimo ?? 10)).length;
        this.stats[2].value = lowStock.toString();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.inventory = [];
        }
      }
    });
  }

  loadVentas() {
    this.http.get<any[]>(`${this.api}/Ventas`, this.getHeaders()).subscribe({
      next: (data) => {
        this.ventas = Array.isArray(data) ? data : [];
        const totalMes = this.ventas.reduce((acc, v) => acc + (v.total || 0), 0);
        this.stats[0].value = `L. ${totalMes.toLocaleString('en-US', { minimumFractionDigits: 2 })}`;
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.ventas = [];
        }
      }
    });
  }

  loadClientes() {
    this.http.get<any[]>(`${this.api}/Clientes`, this.getHeaders()).subscribe({
      next: (data) => {
        this.clientes = Array.isArray(data) ? data : [];
        if (this.clientes.length > 0) {
          if (!this.nuevaVenta.clienteId) this.nuevaVenta.clienteId = this.clientes[0].clienteId;
          if (!this.facturaForm.clienteId) this.facturaForm.clienteId = this.clientes[0].clienteId;
        }
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.clientes = [];
        }
      }
    });
  }

  loadProveedores() {
    this.http.get<any[]>(`${this.api}/Proveedores`, this.getHeaders()).subscribe({
      next: (data) => {
        this.proveedores = Array.isArray(data) ? data : [];
        if (this.proveedores.length > 0 && !this.nuevaCompra.proveedorId) {
          this.nuevaCompra.proveedorId = this.proveedores[0].proveedorId;
        }
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.proveedores = [];
        }
      }
    });
  }

  loadCompras() {
    this.http.get<any[]>(`${this.api}/Compras`, this.getHeaders()).subscribe({
      next: (data) => {
        this.compras = Array.isArray(data) ? data : [];
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.compras = [];
        }
      }
    });
  }

  loadMovimientos() {
    this.http.get<any[]>(`${this.api}/Inventario/Movimientos`, this.getHeaders()).subscribe({
      next: (data) => {
        this.movimientosKardex = Array.isArray(data) ? data : [];
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.movimientosKardex = [];
        }
      }
    });
  }

  loadCajaEstado() {
    this.http.get<any>(`${this.api}/Caja/actual/1`, this.getHeaders()).subscribe({
      next: (data) => {
        this.estadoCaja = data || { estado: 'Cerrado' };
        this.stats[3].value = this.estadoCaja.estado === 'Abierto' ? 'Abierta' : 'Cerrada';
      },
      error: (err) => {
        this.estadoCaja = { estado: 'Cerrado' };
        this.stats[3].value = 'Cerrada';
      }
    });
  }

  loadCajaHistorial() {
    this.http.get<any[]>(`${this.api}/Caja/historial/1`, this.getHeaders()).subscribe({
      next: (data) => {
        this.historialCaja = Array.isArray(data) ? data : [];
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.historialCaja = [];
        }
      }
    });
  }

  loadUsuarios() {
    if (!this.isAdmin) return;
    this.http.get<any[]>(`${this.api}/Acceso/Usuarios`, this.getHeaders()).subscribe({
      next: (data) => {
        this.usuarios = Array.isArray(data) ? data : [];
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.usuarios = [];
        }
      }
    });
  }

  loadPerfiles() {
    if (!this.isAdmin) return;
    this.http.get<any[]>(`${this.api}/Acceso/Perfiles`, this.getHeaders()).subscribe({
      next: (data) => {
        this.perfiles = Array.isArray(data) ? data : [];
      },
      error: () => {
        this.perfiles = [];
      }
    });
  }

  loadSARConfig() {
    this.http.get<SARConfig>(`${this.api}/SAR/configuracion/1`, this.getHeaders()).subscribe({
      next: (data) => {
        this.sarConfig = data;
        if (data) {
          this.sarForm.rtn = data.rtn || this.sarForm.rtn;
          this.sarForm.cai = data.cai || this.sarForm.cai;
          this.sarForm.rangoDesde = data.rangoDesde || this.sarForm.rangoDesde;
          this.sarForm.rangoHasta = data.rangoHasta || this.sarForm.rangoHasta;
          if (data.fechaLimiteEmision) {
            this.sarForm.fechaLimiteEmision = data.fechaLimiteEmision.split('T')[0];
          }
        }
      },
      error: () => {
        this.sarConfig = null;
      }
    });
  }

  guardarSARConfig() {
    if (!this.isAdmin) {
      alert('Acceso denegado: Únicamente los usuarios administradores pueden modificar los parámetros del SAR.');
      return;
    }

    const payload = {
      sucursalId: 1,
      rtn: this.sarForm.rtn.trim(),
      cai: this.sarForm.cai.trim(),
      rangoDesde: this.sarForm.rangoDesde.trim(),
      rangoHasta: this.sarForm.rangoHasta.trim(),
      fechaLimiteEmision: new Date(this.sarForm.fechaLimiteEmision).toISOString(),
      resetearCorrelativo: this.sarForm.resetearCorrelativo
    };

    this.http.post<any>(`${this.api}/SAR/configuracion`, payload, this.getHeaders()).subscribe({
      next: () => {
        alert('¡Configuración de SAR y CAI guardada exitosamente!');
        this.showSARConfigModal = false;
        this.loadSARConfig();
        this.loadVentas();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al guardar configuración SAR: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  // Facturación en vivo
  get clienteSeleccionado(): Cliente | undefined {
    return this.clientes.find(c => c.clienteId == this.facturaForm.clienteId);
  }

  get metodoPagoNombre(): string {
    switch (Number(this.facturaForm.metodoPagoId)) {
      case 1: return 'Efectivo';
      case 2: return 'Tarjeta de Crédito / Débito';
      case 3: return 'Transferencia Bancaria';
      default: return 'Efectivo';
    }
  }

  get ultimaFacturaDisplay(): string {
    return this.ventas.find(v => v.numeroFactura)?.numeroFactura || '000-001-01-00000000';
  }

  get nuevaFacturaDisplay(): string {
    return this.sarConfig?.siguienteFactura || '000-001-01-00000001';
  }

  onProductoCambio(item: FacturaItemRow) {
    const p = this.products.find(x => x.productoId == item.productoId);
    if (p) {
      item.nombre = p.nombre || '';
      item.precio = p.precioVenta || p.precio || 0;
      item.tipoISV = p.tipoISV || 'ISV15';
    }
  }

  agregarItemFactura() {
    const firstProd = this.products[0];
    this.facturaForm.items.push({
      productoId: firstProd?.productoId || 1,
      nombre: firstProd?.nombre || 'Producto',
      cantidad: 1,
      precio: firstProd?.precioVenta || firstProd?.precio || 0,
      descuento: 0,
      tipoISV: firstProd?.tipoISV || 'ISV15'
    });
  }

  eliminarItemFactura(index: number) {
    if (this.facturaForm.items.length > 1) {
      this.facturaForm.items.splice(index, 1);
    }
  }

  calcularLineaTotal(item: FacturaItemRow): number {
    const sub = (item.precio || 0) * (item.cantidad || 0);
    return Math.max(0, sub - (item.descuento || 0));
  }

  get subtotalFactura(): number {
    return this.facturaForm.items.reduce((acc, i) => acc + this.calcularLineaTotal(i), 0);
  }

  get gravado15Factura(): number {
    return this.facturaForm.items
      .filter(i => i.tipoISV === 'ISV15')
      .reduce((acc, i) => acc + this.calcularLineaTotal(i), 0);
  }

  get gravado18Factura(): number {
    return this.facturaForm.items
      .filter(i => i.tipoISV === 'ISV18')
      .reduce((acc, i) => acc + this.calcularLineaTotal(i), 0);
  }

  get exentoFactura(): number {
    return this.facturaForm.items
      .filter(i => i.tipoISV === 'EXENTO')
      .reduce((acc, i) => acc + this.calcularLineaTotal(i), 0);
  }

  get isv15Factura(): number {
    return this.gravado15Factura * 0.15;
  }

  get isv18Factura(): number {
    return this.gravado18Factura * 0.18;
  }

  get totalFactura(): number {
    return this.subtotalFactura + this.isv15Factura + this.isv18Factura;
  }

  get totalEnLetrasFactura(): string {
    return this.convertirNumeroALetras(this.totalFactura);
  }

  convertirNumeroALetras(monto: number): string {
    if (monto <= 0) return 'CERO LEMPIRAS CON 00/100';
    const centavos = Math.round((monto % 1) * 100);
    const enteros = Math.floor(monto);
    const centavosStr = centavos < 10 ? `0${centavos}` : `${centavos}`;
    return `${this.numeroALetras(enteros)} LEMPIRAS CON ${centavosStr}/100`;
  }

  private numeroALetras(n: number): string {
    if (n === 0) return 'CERO';
    if (n === 1) return 'UN';
    const unidades = ['', 'UN', 'DOS', 'TRES', 'CUATRO', 'CINCO', 'SEIS', 'SIETE', 'OCHO', 'NUEVE'];
    const decenas = ['', 'DIEZ', 'VEINTE', 'TREINTA', 'CUARENTA', 'CINCUENTA', 'SESENTA', 'SETENTA', 'OCHENTA', 'NOVENTA'];
    const dieces = ['DIEZ', 'ONCE', 'DOCE', 'TRECE', 'CATORCE', 'QUINCE', 'DIECISÉIS', 'DIECISIETE', 'DIECIOCHO', 'DIECINUEVE'];
    const veintes = ['VEINTE', 'VEINTIUNO', 'VEINTIDÓS', 'VEINTITRÉS', 'VEINTICUATRO', 'VEINTICINCO', 'VEINTISÉIS', 'VEINTISIETE', 'VEINTIOCHO', 'VEINTINUEVE'];
    const centenas = ['', 'CIENTO', 'DOSCIENTOS', 'TRESCIENTOS', 'CUATROCIENTOS', 'QUINIENTOS', 'SEISCIENTOS', 'SETECIENTOS', 'OCHOCIENTOS', 'NOVECIENTOS'];

    if (n < 10) return unidades[n];
    if (n >= 10 && n < 20) return dieces[n - 10];
    if (n >= 20 && n < 30) return veintes[n - 20];
    if (n >= 30 && n < 100) {
      const d = Math.floor(n / 10);
      const u = n % 10;
      return u === 0 ? decenas[d] : `${decenas[d]} Y ${unidades[u]}`;
    }
    if (n === 100) return 'CIEN';
    if (n > 100 && n < 1000) {
      const c = Math.floor(n / 100);
      const resto = n % 100;
      return `${centenas[c]} ${this.numeroALetras(resto)}`;
    }
    if (n === 1000) return 'UN MIL';
    if (n > 1000 && n < 2000) {
      return `UN MIL ${this.numeroALetras(n % 1000)}`;
    }
    if (n >= 2000 && n < 1000000) {
      const miles = Math.floor(n / 1000);
      const resto = n % 1000;
      return resto === 0 ? `${this.numeroALetras(miles)} MIL` : `${this.numeroALetras(miles)} MIL ${this.numeroALetras(resto)}`;
    }
    return n.toString();
  }

  generarFacturaEnVivo() {
    if (this.facturaForm.items.length === 0) {
      alert('Agrega al menos un producto a la factura.');
      return;
    }

    this.generandoFactura = true;
    const payload = {
      clienteId: Number(this.facturaForm.clienteId),
      sucursalId: 1,
      usuarioId: 1,
      metodoPagoId: Number(this.facturaForm.metodoPagoId),
      concretarVenta: true,
      emitirFactura: true,
      detalles: this.facturaForm.items.map(i => ({
        productoId: Number(i.productoId),
        cantidad: Number(i.cantidad),
        precioUnitario: Number(i.precio),
        descuento: Number(i.descuento || 0)
      }))
    };

    this.http.post<any>(`${this.api}/Ventas`, payload, this.getHeaders()).subscribe({
      next: (ventaCreada) => {
        this.generandoFactura = false;
        this.facturaEmitida = true;
        this.facturaEmitidaData = ventaCreada;
        alert(`¡Factura Fiscal Nº ${ventaCreada.numeroFactura || 'Generada'} emitida con éxito! Se descontó el inventario y se asignó el correlativo SAR.`);
        this.loadVentas();
        this.loadInventory();
        this.loadSARConfig();
        this.loadCajaEstado();
      },
      error: (err) => {
        this.generandoFactura = false;
        if (!this.handleAuthError(err)) {
          alert('Error al generar la factura: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  nuevaFacturaFormulario() {
    this.facturaEmitida = false;
    this.facturaEmitidaData = null;
    const firstProd = this.products[0];
    this.facturaForm = {
      clienteId: this.clientes[0]?.clienteId || 1,
      fecha: new Date().toISOString().split('T')[0],
      metodoPagoId: 1,
      moneda: 'Lempiras (HNL)',
      notas: '',
      notasPie: 'Gracias por su compra en Sistema Vision Tech. Garantía de 6 meses en monturas.',
      mostrarNotas: false,
      mostrarNotasPie: false,
      items: [
        {
          productoId: firstProd?.productoId || 1,
          nombre: firstProd?.nombre || 'Producto',
          cantidad: 1,
          precio: firstProd?.precioVenta || firstProd?.precio || 0,
          descuento: 0,
          tipoISV: firstProd?.tipoISV || 'ISV15'
        }
      ]
    };
  }

  imprimirFacturaEnVivo() {
    window.print();
  }

  resetearNuevaFactura() {
    this.nuevaFacturaFormulario();
  }

  generarFacturaFiscal() {
    this.generarFacturaEnVivo();
  }

  // =========================================================================
  // GESTIÓN DE COMPRAS Y PROVEEDORES
  // =========================================================================
  abrirModalNuevaCompra() {
    if (this.proveedores.length === 0) {
      alert('Debes registrar al menos un proveedor antes de crear una orden de compra.');
      this.abrirModalNuevoProveedor();
      return;
    }
    const firstProd = this.products[0];
    this.nuevaCompra = {
      proveedorId: this.proveedores[0].proveedorId,
      items: [
        {
          productoId: firstProd?.productoId || 1,
          cantidad: 10,
          precioUnitario: (firstProd?.precioVenta || 1000) * 0.6
        }
      ]
    };
    this.showNuevaCompraModal = true;
  }

  agregarItemCompra() {
    const firstProd = this.products[0];
    this.nuevaCompra.items.push({
      productoId: firstProd?.productoId || 1,
      cantidad: 5,
      precioUnitario: 500
    });
  }

  eliminarItemCompra(index: number) {
    if (this.nuevaCompra.items.length > 1) {
      this.nuevaCompra.items.splice(index, 1);
    }
  }

  calcularTotalCompra(): number {
    return this.nuevaCompra.items.reduce((acc: number, i: any) => acc + (i.cantidad * i.precioUnitario), 0);
  }

  guardarCompra() {
    if (this.nuevaCompra.items.length === 0) {
      alert('Agrega al menos un producto a la compra.');
      return;
    }

    this.guardandoCompra = true;
    const payload = {
      proveedorId: Number(this.nuevaCompra.proveedorId),
      detalles: this.nuevaCompra.items.map((i: any) => ({
        productoId: Number(i.productoId),
        cantidad: Number(i.cantidad),
        precioUnitario: Number(i.precioUnitario)
      }))
    };

    this.http.post<any>(`${this.api}/Compras`, payload, this.getHeaders()).subscribe({
      next: (compraCreada) => {
        this.guardandoCompra = false;
        this.showNuevaCompraModal = false;
        alert(`¡Orden de Compra #${compraCreada.compraId} registrada exitosamente!`);
        this.loadCompras();
      },
      error: (err) => {
        this.guardandoCompra = false;
        if (!this.handleAuthError(err)) {
          alert('Error al registrar compra: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  recibirCompra(compraId: number) {
    if (!confirm('¿Deseas marcar esta orden como RECIBIDA? El inventario se actualizará automáticamente con las unidades ingresadas.')) {
      return;
    }

    this.http.put<any>(`${this.api}/Compras/${compraId}/Recibir`, {}, this.getHeaders()).subscribe({
      next: () => {
        alert('¡Mercancía recibida e inventario actualizado exitosamente!');
        this.loadCompras();
        this.loadInventory();
        this.loadProducts();
        this.loadMovimientos();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al recibir compra: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  anularCompra(compraId: number) {
    if (!confirm('¿Estás seguro de anular esta orden de compra?')) {
      return;
    }

    this.http.put<any>(`${this.api}/Compras/${compraId}/Anular`, {}, this.getHeaders()).subscribe({
      next: () => {
        alert('Orden de compra anulada.');
        this.loadCompras();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al anular compra: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  abrirModalNuevoProveedor() {
    this.proveedorEditandoId = null;
    this.proveedorForm = {
      nombre: '',
      contacto: '',
      telefono: '',
      email: '',
      direccion: 'Tegucigalpa, Honduras',
      rtn: ''
    };
    this.showProveedorModal = true;
  }

  abrirModalEditarProveedor(prov: Proveedor) {
    this.proveedorEditandoId = prov.proveedorId;
    this.proveedorForm = {
      nombre: prov.nombre,
      contacto: prov.contacto || '',
      telefono: prov.telefono || '',
      email: prov.email || '',
      direccion: prov.direccion || '',
      rtn: prov.rtn || ''
    };
    this.showProveedorModal = true;
  }

  guardarProveedor() {
    if (!this.proveedorForm.nombre || !this.proveedorForm.nombre.trim()) {
      alert('El nombre del proveedor es obligatorio.');
      return;
    }

    this.guardandoProveedor = true;
    const payload = {
      nombre: this.proveedorForm.nombre.trim(),
      contacto: this.proveedorForm.contacto?.trim() || '',
      telefono: this.proveedorForm.telefono?.trim() || '',
      email: this.proveedorForm.email?.trim() || '',
      direccion: this.proveedorForm.direccion?.trim() || '',
      rtn: this.proveedorForm.rtn?.trim() || ''
    };

    const req$ = this.proveedorEditandoId
      ? this.http.put<any>(`${this.api}/Proveedores/${this.proveedorEditandoId}`, payload, this.getHeaders())
      : this.http.post<any>(`${this.api}/Proveedores`, payload, this.getHeaders());

    req$.subscribe({
      next: () => {
        this.guardandoProveedor = false;
        this.showProveedorModal = false;
        alert(this.proveedorEditandoId ? 'Proveedor actualizado con éxito.' : '¡Proveedor creado exitosamente!');
        this.loadProveedores();
      },
      error: (err) => {
        this.guardandoProveedor = false;
        if (!this.handleAuthError(err)) {
          alert('Error al guardar proveedor: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  eliminarProveedor(id: number) {
    if (!confirm('¿Estás seguro de eliminar este proveedor?')) return;
    this.http.delete<any>(`${this.api}/Proveedores/${id}`, this.getHeaders()).subscribe({
      next: () => {
        alert('Proveedor eliminado.');
        this.loadProveedores();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al eliminar proveedor: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  // =========================================================================
  // GESTIÓN DE CAJA (ARQUEO Y TURNOS)
  // =========================================================================
  abrirModalAbrirCaja() {
    this.montoAperturaInput = 1000;
    this.showAbrirCajaModal = true;
  }

  confirmarAbrirCaja() {
    const payload = {
      sucursalId: 1,
      montoApertura: Number(this.montoAperturaInput)
    };

    this.http.post<any>(`${this.api}/Caja/abrir`, payload, this.getHeaders()).subscribe({
      next: (res) => {
        alert(`¡Caja abierta exitosamente con fondo inicial de L. ${res.montoApertura}!`);
        this.showAbrirCajaModal = false;
        this.loadCajaEstado();
        this.loadCajaHistorial();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al abrir caja: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  abrirModalCerrarCaja() {
    const esperado = (this.estadoCaja.montoApertura || 0) + (this.estadoCaja.totalVentasEfectivo || 0);
    this.montoCierreInput = esperado;
    this.showCerrarCajaModal = true;
  }

  confirmarCerrarCaja() {
    if (!this.estadoCaja.id) {
      alert('No se detectó el identificador de la caja actual.');
      return;
    }

    const payload = {
      montoEfectivoFinal: Number(this.montoCierreInput)
    };

    this.http.post<any>(`${this.api}/Caja/${this.estadoCaja.id}/cerrar`, payload, this.getHeaders()).subscribe({
      next: (res) => {
        const dif = res.diferencia ?? 0;
        const msgDif = dif === 0 ? '¡Arqueo exacto!' : dif > 0 ? `Sobrante: L. ${dif}` : `Faltante: L. ${dif}`;
        alert(`¡Caja cerrada exitosamente! ${msgDif}`);
        this.showCerrarCajaModal = false;
        this.loadCajaEstado();
        this.loadCajaHistorial();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al cerrar caja: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  // =========================================================================
  // ACCIONES EN VENTAS (CONFIRMAR, ANULAR)
  // =========================================================================
  confirmarVenta(ventaId: number) {
    if (!confirm('¿Deseas confirmar esta venta?')) return;
    this.http.put<any>(`${this.api}/Ventas/${ventaId}/Confirmar`, {}, this.getHeaders()).subscribe({
      next: () => {
        alert('Venta confirmada exitosamente.');
        this.loadVentas();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al confirmar venta: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  anularVenta(ventaId: number) {
    if (!confirm('¿Estás seguro de anular esta venta?')) return;
    this.http.put<any>(`${this.api}/Ventas/${ventaId}/Anular`, {}, this.getHeaders()).subscribe({
      next: () => {
        alert('Venta anulada.');
        this.loadVentas();
        this.loadInventory();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al anular venta: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  // =========================================================================
  // GESTIÓN DE USUARIOS (RBAC)
  // =========================================================================
  abrirModalNuevoUsuario() {
    this.nuevoUsuarioForm = {
      nombre: '',
      email: '',
      password: '',
      perfilId: this.perfiles[0]?.perfilId || 1
    };
    this.showNuevoUsuarioModal = true;
  }

  guardarUsuario() {
    if (!this.nuevoUsuarioForm.nombre || !this.nuevoUsuarioForm.email || !this.nuevoUsuarioForm.password) {
      alert('Todos los campos son obligatorios.');
      return;
    }

    this.guardandoUsuario = true;
    const payload = {
      nombre: this.nuevoUsuarioForm.nombre.trim(),
      email: this.nuevoUsuarioForm.email.trim(),
      contraseña: this.nuevoUsuarioForm.password,
      perfilId: Number(this.nuevoUsuarioForm.perfilId)
    };

    this.http.post<any>(`${this.api}/Acceso/Usuarios`, payload, this.getHeaders()).subscribe({
      next: () => {
        this.guardandoUsuario = false;
        this.showNuevoUsuarioModal = false;
        alert('¡Usuario creado exitosamente!');
        this.loadUsuarios();
      },
      error: (err) => {
        this.guardandoUsuario = false;
        if (!this.handleAuthError(err)) {
          alert('Error al crear usuario: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  eliminarUsuario(id: number) {
    if (!confirm('¿Estás seguro de eliminar este usuario?')) return;
    this.http.delete<any>(`${this.api}/Acceso/Usuarios/${id}`, this.getHeaders()).subscribe({
      next: () => {
        alert('Usuario eliminado.');
        this.loadUsuarios();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al eliminar usuario: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  // =========================================================================
  // EDICIÓN Y ELIMINACIÓN DE PRODUCTOS Y CLIENTES
  // =========================================================================
  abrirModalEditarProducto(prod: Product) {
    this.productoEditando = {
      productoId: prod.productoId,
      nombre: prod.nombre,
      descripcion: '',
      codigoBarras: prod.codigoBarras || prod.codigo,
      precio: prod.precioVenta || prod.precio,
      tipoISV: prod.tipoISV || 'ISV15'
    };
    this.showEditarProductoModal = true;
  }

  guardarEdicionProducto() {
    this.guardandoEditarProducto = true;
    const payload = {
      nombre: this.productoEditando.nombre,
      descripcion: this.productoEditando.descripcion || '',
      codigoBarras: this.productoEditando.codigoBarras,
      precio: Number(this.productoEditando.precio),
      tipoISV: this.productoEditando.tipoISV
    };

    this.http.put<any>(`${this.api}/Productos/${this.productoEditando.productoId}`, payload, this.getHeaders()).subscribe({
      next: () => {
        this.guardandoEditarProducto = false;
        this.showEditarProductoModal = false;
        alert('Producto actualizado con éxito.');
        this.loadProducts();
      },
      error: (err) => {
        this.guardandoEditarProducto = false;
        if (!this.handleAuthError(err)) {
          alert('Error al actualizar producto: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  eliminarProducto(id: number) {
    if (!confirm('¿Estás seguro de eliminar este producto?')) return;
    this.http.delete<any>(`${this.api}/Productos/${id}`, this.getHeaders()).subscribe({
      next: () => {
        alert('Producto eliminado.');
        this.loadProducts();
        this.loadInventory();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al eliminar producto: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  abrirModalEditarCliente(cli: Cliente) {
    this.clienteEditando = {
      clienteId: cli.clienteId,
      nombre: cli.nombre,
      rtn: cli.rtn || '',
      telefono: cli.telefono || '',
      email: cli.email || '',
      direccion: cli.direccion || 'Tegucigalpa, Honduras',
      tipoCliente: cli.tipoCliente || 'Natural'
    };
    this.showEditarClienteModal = true;
  }

  guardarEdicionCliente() {
    this.guardandoEditarCliente = true;
    const payload = {
      nombre: this.clienteEditando.nombre,
      rtn: this.clienteEditando.rtn,
      telefono: this.clienteEditando.telefono,
      email: this.clienteEditando.email,
      direccion: this.clienteEditando.direccion,
      tipoCliente: this.clienteEditando.tipoCliente
    };

    this.http.put<any>(`${this.api}/Clientes/${this.clienteEditando.clienteId}`, payload, this.getHeaders()).subscribe({
      next: () => {
        this.guardandoEditarCliente = false;
        this.showEditarClienteModal = false;
        alert('Cliente actualizado con éxito.');
        this.loadClientes();
      },
      error: (err) => {
        this.guardandoEditarCliente = false;
        if (!this.handleAuthError(err)) {
          alert('Error al actualizar cliente: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  eliminarCliente(id: number) {
    if (!confirm('¿Estás seguro de eliminar este cliente?')) return;
    this.http.delete<any>(`${this.api}/Clientes/${id}`, this.getHeaders()).subscribe({
      next: () => {
        alert('Cliente eliminado.');
        this.loadClientes();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al eliminar cliente: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  // =========================================================================
  // CREACIÓN RÁPIDA DE CLIENTES, PRODUCTOS Y MOVIMIENTOS
  // =========================================================================
  abrirModalNuevoCliente() {
    this.nuevoCliente = {
      nombre: '',
      rtn: '',
      telefono: '',
      email: '',
      direccion: 'Tegucigalpa, Honduras',
      tipoCliente: 'Natural'
    };
    this.showNuevoClienteModal = true;
  }

  guardarCliente() {
    if (!this.nuevoCliente.nombre || !this.nuevoCliente.nombre.trim()) {
      alert('El nombre del cliente es obligatorio.');
      return;
    }

    const payload = {
      nombre: this.nuevoCliente.nombre.trim(),
      rtn: this.nuevoCliente.rtn?.trim() || '00000000000000',
      telefono: this.nuevoCliente.telefono?.trim() || '',
      email: this.nuevoCliente.email?.trim() || '',
      direccion: this.nuevoCliente.direccion?.trim() || 'Tegucigalpa, Honduras',
      tipoCliente: this.nuevoCliente.tipoCliente || 'Natural'
    };

    this.http.post<any>(`${this.api}/Clientes`, payload, this.getHeaders()).subscribe({
      next: (clienteCreado) => {
        alert(`¡Cliente "${clienteCreado.nombre}" registrado exitosamente!`);
        this.showNuevoClienteModal = false;
        this.loadClientes();
        if (clienteCreado.clienteId) {
          this.facturaForm.clienteId = clienteCreado.clienteId;
          this.nuevaVenta.clienteId = clienteCreado.clienteId;
        }
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al registrar cliente: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  abrirModalMovimientoInventario() {
    if (this.products.length > 0) {
      this.movimientoInventario.productoId = this.products[0].productoId!;
    }
    this.movimientoInventario.tipoMovimiento = 'ENTRADA';
    this.movimientoInventario.cantidad = 10;
    this.showMovimientoInventarioModal = true;
  }

  guardarMovimientoInventario() {
    if (this.movimientoInventario.cantidad <= 0) {
      alert('La cantidad a mover debe ser mayor a 0.');
      return;
    }

    const payload = {
      productoId: Number(this.movimientoInventario.productoId),
      tipoMovimiento: this.movimientoInventario.tipoMovimiento,
      cantidad: Number(this.movimientoInventario.cantidad)
    };

    this.http.post<any>(`${this.api}/Inventario/Movimiento`, payload, this.getHeaders()).subscribe({
      next: () => {
        alert('¡Movimiento de inventario registrado con éxito!');
        this.showMovimientoInventarioModal = false;
        this.loadInventory();
        this.loadProducts();
        this.loadMovimientos();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al registrar movimiento: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  abrirModalNuevoProducto() {
    this.nuevoProducto = {
      nombre: '',
      descripcion: '',
      codigoBarras: 'OPT-' + Math.floor(1000 + Math.random() * 9000),
      precio: 1200,
      tipoISV: 'ISV15',
      tieneNumeroSerie: false,
      tieneLote: false,
      stockInicial: 25
    };
    this.showNuevoProductoModal = true;
  }

  guardarProducto() {
    if (!this.nuevoProducto.nombre || !this.nuevoProducto.nombre.trim()) {
      alert('El nombre del producto es obligatorio.');
      return;
    }

    this.guardandoProducto = true;
    const payload = {
      nombre: this.nuevoProducto.nombre.trim(),
      descripcion: this.nuevoProducto.descripcion?.trim() || '',
      codigoBarras: this.nuevoProducto.codigoBarras?.trim() || 'OPT-' + Math.floor(1000 + Math.random() * 9000),
      precio: Number(this.nuevoProducto.precio),
      tipoISV: this.nuevoProducto.tipoISV || 'ISV15',
      tieneNumeroSerie: this.nuevoProducto.tieneNumeroSerie,
      tieneLote: this.nuevoProducto.tieneLote,
      stockInicial: Number(this.nuevoProducto.stockInicial || 0)
    };

    this.http.post<any>(`${this.api}/Productos`, payload, this.getHeaders()).subscribe({
      next: (prodCreado) => {
        this.guardandoProducto = false;
        this.showNuevoProductoModal = false;
        alert(`¡Producto "${prodCreado.nombre}" creado exitosamente con stock de ${payload.stockInicial} unidades! Ya está disponible para la venta.`);
        this.loadProducts();
        this.loadInventory();
      },
      error: (err) => {
        this.guardandoProducto = false;
        if (!this.handleAuthError(err)) {
          alert('Error al registrar producto: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  abrirSARConfig() {
    if (!this.isAdmin) {
      alert('Acceso restringido: Solo administradores pueden configurar los datos del SAR.');
      return;
    }
    this.showSARConfigModal = true;
  }

  verFactura(venta: any) {
    this.http.get<any>(`${this.api}/Ventas/${venta.ventaId}/Factura`, this.getHeaders()).subscribe({
      next: (factura) => {
        this.facturaSeleccionada = {
          ...factura,
          venta: venta
        };
        this.showFacturaModal = true;
      },
      error: () => {
        if (venta.numeroFactura && venta.cai) {
          this.facturaSeleccionada = {
            numeroFactura: venta.numeroFactura,
            cai: venta.cai,
            fechaEmision: venta.fechaFactura || venta.fechaVenta,
            nombreCliente: venta.cliente,
            rtnCliente: venta.clienteRtn || 'Consumidor Final',
            total: venta.total,
            montoExento: venta.montoExento || 0,
            montoGravado15: venta.montoGravado15 || (venta.total / 1.15),
            isv15: venta.isv15 || (venta.total - (venta.total / 1.15)),
            rangoDesde: this.sarConfig?.rangoDesde || '000-001-01-00000001',
            rangoHasta: this.sarConfig?.rangoHasta || '000-001-01-00005000',
            fechaLimiteEmision: this.sarConfig?.fechaLimiteEmision || '2027-12-31',
            venta: venta
          };
          this.showFacturaModal = true;
        } else {
          alert('Esta venta aún no tiene una factura fiscal emitida.');
        }
      }
    });
  }

  emitirFactura(ventaId: number) {
    this.http.post<any>(`${this.api}/Ventas/${ventaId}/Facturar?sucursalId=1`, {}, this.getHeaders()).subscribe({
      next: (factura) => {
        alert(`¡Factura emitida correctamente! Nº: ${factura.numeroFactura}`);
        this.loadVentas();
        this.loadSARConfig();
        this.verFactura({ ventaId, ...factura });
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          alert('Error al emitir factura: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  iconFor(name: string) {
    return this.icons[name as keyof typeof this.icons];
  }

  get filteredProducts() {
    return this.products.filter(p =>
      `${p.nombre} ${p.codigo}`.toLowerCase().includes(this.search.toLowerCase())
    );
  }

  get filteredVentas() {
    return this.ventas.filter(v =>
      `${v.cliente} ${v.numeroFactura || ''} ${v.cai || ''}`.toLowerCase().includes(this.search.toLowerCase())
    );
  }

  get recentSales() {
    return this.ventas.slice(0, 5);
  }
}
