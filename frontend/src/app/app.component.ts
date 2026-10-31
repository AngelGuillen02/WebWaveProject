import { environment } from '../environments/environment';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, inject, HostListener } from '@angular/core';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';
import * as XLSX from 'xlsx';
import html2canvas from 'html2canvas';
import { jsPDF } from 'jspdf';
import {
  Bell, Boxes, ChevronDown, CircleDollarSign, ClipboardList,
  FileBarChart, Home, LogOut, Menu, Package, Search,
  Settings, ShoppingCart, Store, Truck, Users, X, FileText, CheckCircle2,
  AlertTriangle, Plus, Lock, Printer, Trash2, Check, Shield, RotateCcw,
  Pencil, History, Ban, Layers, Download, LucideAngularModule
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
  esCotizacion?: boolean;
  numeroCotizacion?: string;
  numeroFactura?: string;
  cai?: string;
  isv15?: number;
  isv18?: number;
  montoExento?: number;
  montoGravado15?: number;
  montoGravado18?: number;
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
    Pencil, History, Ban, Layers, Download
  };

  readonly api = environment.apiUrl;
  loginMode = !sessionStorage.getItem('webwave_token');
  loading = false;
  error = '';
  credentials = { email: '', password: '' };
  active = 'Resumen';
  search = '';
  sidebarOpen = false;

  currentUser: {
    name: string;
    email?: string;
    role: string;
    initials: string;
  } = {
    name: sessionStorage.getItem('webwave_user_name') || 'Angel Guillen',
    email: sessionStorage.getItem('webwave_user_email') || 'admin@visiontech.com',
    role: sessionStorage.getItem('webwave_user_role') || 'Administrador',
    initials: sessionStorage.getItem('webwave_user_initials') || 'AG'
  };

  get isAdmin(): boolean {
    const role = (this.currentUser.role || '').toLowerCase();
    return role.includes('admin') || role === 'administrador';
  }

  get isFacturador(): boolean {
    const role = (this.currentUser.role || '').toLowerCase();
    return role.includes('factur') || role.includes('ventas') || role.includes('vendedor');
  }

  get isCajero(): boolean {
    const role = (this.currentUser.role || '').toLowerCase();
    return role.includes('cajer');
  }

  get isAlmacen(): boolean {
    const role = (this.currentUser.role || '').toLowerCase();
    return role.includes('almacen') || role.includes('inventario');
  }

  puedeVerModulo(modulo: string): boolean {
    if (this.isAdmin) return true;

    // Rol Facturación y Ventas: ve únicamente Resumen, Facturas (ventas), Clientes y Productos
    if (this.isFacturador) {
      return ['Resumen', 'Facturas', 'Clientes', 'Productos'].includes(modulo);
    }

    // Rol Cajero: ve Resumen, Facturas, Caja, Clientes, Productos
    if (this.isCajero) {
      return ['Resumen', 'Facturas', 'Caja', 'Clientes', 'Productos'].includes(modulo);
    }

    // Rol Almacén: ve Resumen, Inventario, Productos, Compras, Proveedores
    if (this.isAlmacen) {
      return ['Resumen', 'Inventario', 'Productos', 'Compras', 'Proveedores'].includes(modulo);
    }

    // Rol general por defecto
    return ['Resumen', 'Facturas', 'Clientes', 'Productos'].includes(modulo);
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

  // Pestañas secundarias en Facturas e Inventario
  subTabFacturas: 'listado' | 'crear' = 'listado';
  subTabInventario: 'stock' | 'kardex' = 'stock';

  abrirCrearFactura() {
    this.active = 'Facturas';
    this.subTabFacturas = 'crear';
  }

  volverAListadoFacturas() {
    this.subTabFacturas = 'listado';
    this.loadVentas();
  }

  // Configuración SAR
  sarConfig: SARConfig | null = null;
  showSARConfigModal = false;
  showFacturaModal = false;
  showNuevoProductoModal = false;
  showNuevoClienteModal = false;
  showMovimientoInventarioModal = false;
  facturaSeleccionada: any = null;

  // Modales de Cotizaciones y Auditoría
  showEditarCotizacionModal = false;
  guardandoEdicionCotizacion = false;
  cotizacionEditando: any = null;
  editarCotizacionForm: {
    clienteId: number;
    motivo: string;
    items: FacturaItemRow[];
  } = {
    clienteId: 1,
    motivo: '',
    items: []
  };

  showHistorialCotizacionModal = false;
  cargandoHistorial = false;
  historialCotizacionList: any[] = [];
  cotizacionHistorialVenta: any = null;

  showVerCotizacionModal = false;
  cotizacionSeleccionada: any = null;

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

  // Pestañas secundarias de Configuración
  subTabConfiguracion: 'general' | 'facturas' | 'seguridad' | 'auditoria' = 'general';
  
  // Cambio de contraseña
  seguridadPassword = {
    passwordActual: '',
    nuevaPassword: '',
    confirmarPassword: ''
  };
  guardandoPassword = false;

  // Bitácora global de auditoría (Solo Administrador)
  logsAuditoriaList: any[] = [];
  cargandoAuditoria = false;
  guardandoConfiguracion = false;
  companyConfig = {
    nombre: localStorage.getItem('vt_company_nombre') || '3tech Distribuidora S. De R.L de C.V.',
    nombreGerente: localStorage.getItem('vt_company_gerente_nombre') || 'Angel',
    apellidoGerente: localStorage.getItem('vt_company_gerente_apellido') || 'Guillen',
    rtn: localStorage.getItem('vt_company_rtn') || '18049025265441',
    direccion: localStorage.getItem('vt_company_direccion') || '4ta Calle sur del progreso, Enfrente de Construcenter, mano derecho de Aire frio, Ala par de Carwash Osman',
    telefono: localStorage.getItem('vt_company_telefono') || '0000-0000',
    email: localStorage.getItem('vt_company_email') || 'visiontech3x@gmail.com',
    notaDocumento: localStorage.getItem('vt_company_nota_doc') || 'Gracias por su preferencia. Los pagos se realizan a 30 días.',
    notaPie: localStorage.getItem('vt_company_nota_pie') || 'Garantía de 1 año en equipos de seguridad electrónica y tecnología. Conserve este comprobante.',
    logoUrl: localStorage.getItem('vt_company_logo') || 'assets/logo.png'
  };

  // Modal de Historial de Auditoría de Producto
  showHistorialProductoModal = false;
  cargandoHistorialProducto = false;
  productoHistorialSeleccionado: any = null;
  historialProductoList: any[] = [];

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


  // Cierre de cualquier modal al presionar la tecla Esc
  @HostListener('window:keydown.escape', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) {
    this.cerrarTodosLosModales();
  }

  cerrarTodosLosModales() {
    this.showSARConfigModal = false;
    this.showFacturaModal = false;
    this.showNuevoProductoModal = false;
    this.showNuevoClienteModal = false;
    this.showMovimientoInventarioModal = false;
    this.showEditarCotizacionModal = false;
    this.showHistorialCotizacionModal = false;
    this.showVerCotizacionModal = false;
    this.showNuevaCompraModal = false;
    this.showProveedorModal = false;
    this.showAbrirCajaModal = false;
    this.showCerrarCajaModal = false;
    this.showNuevoUsuarioModal = false;
    this.showHistorialProductoModal = false;
    this.showEditarProductoModal = false;
    this.showEditarClienteModal = false;
  }

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
    tipoDocumento: 'Factura' as 'Factura' | 'Cotizacion',
    clienteId: 1,
    fecha: new Date().toISOString().split('T')[0],
    metodoPagoId: 1,
    moneda: 'Lempiras (HNL)',
    notas: '',
    notasPie: 'Gracias por su compra en Sistema Vision Tech. Garantía de 1 año en equipos de seguridad electrónica y redes.',
    mostrarNotas: false,
    mostrarNotasPie: false,
    items: [
      {
        productoId: 7,
        nombre: 'Camara Domo IP 4MP IR 30m PoE',
        cantidad: 1,
        precio: 1450.00,
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
    { label: 'Facturas', icon: 'FileText' },
    { label: 'Compras', icon: 'Truck' },
    { label: 'Inventario', icon: 'Boxes' },
    { label: 'Productos', icon: 'Package' },
    { label: 'Clientes', icon: 'Users' },
    { label: 'Proveedores', icon: 'Store' },
    { label: 'Caja', icon: 'CircleDollarSign' },
    { label: 'Reportes', icon: 'FileBarChart' },
    { label: 'Usuarios', icon: 'Shield' },
    { label: 'Configuración', icon: 'Settings' }
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

  // =========================================================================
  // NOTIFICACIONES Y ALERTAS (SWEETALERT2)
  // =========================================================================
  notificarExito(titulo: string, texto?: string) {
    return Swal.fire({
      icon: 'success',
      title: titulo,
      text: texto || '',
      confirmButtonColor: '#1f8a67',
      confirmButtonText: 'Aceptar',
      timer: 3500,
      timerProgressBar: true
    });
  }

  notificarError(titulo: string, texto?: string) {
    return Swal.fire({
      icon: 'error',
      title: titulo,
      text: texto || '',
      confirmButtonColor: '#dc2626',
      confirmButtonText: 'Entendido'
    });
  }

  notificarAdvertencia(titulo: string, texto?: string) {
    return Swal.fire({
      icon: 'warning',
      title: titulo,
      text: texto || '',
      confirmButtonColor: '#e98c35',
      confirmButtonText: 'Aceptar'
    });
  }

  notificarInfo(titulo: string, texto?: string) {
    return Swal.fire({
      icon: 'info',
      title: titulo,
      text: texto || '',
      confirmButtonColor: '#12352d',
      confirmButtonText: 'Aceptar'
    });
  }

  async confirmarAccion(titulo: string, texto: string, confirmText: string = 'Sí, continuar', cancelText: string = 'Cancelar', esPeligroso: boolean = false): Promise<boolean> {
    const result = await Swal.fire({
      title: titulo,
      text: texto,
      icon: esPeligroso ? 'warning' : 'question',
      showCancelButton: true,
      confirmButtonColor: esPeligroso ? '#dc2626' : '#1f8a67',
      cancelButtonColor: '#64748b',
      confirmButtonText: confirmText,
      cancelButtonText: cancelText
    });
    return result.isConfirmed;
  }

  private handleAuthError(err: any): boolean {
    if (err.status === 401) {
      this.notificarAdvertencia('Sesión Expirada', 'Tu sesión ha expirado o el token de seguridad no es válido. Por favor vuelve a iniciar sesión.');
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
          const userEmail = this.credentials.email || 'admin@visiontech.com';
          sessionStorage.setItem('webwave_user_email', userEmail);
          this.currentUser = {
            name: response.nombre,
            email: userEmail,
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
    if (label === 'Facturas' || label === 'Facturación' || label === 'Ventas') {
      this.subTabFacturas = 'listado';
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
      this.notificarAdvertencia('Acceso denegado', 'Únicamente los usuarios administradores pueden modificar los parámetros del SAR.');
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
        this.notificarExito('Configuración Guardada', '¡Configuración de SAR y CAI guardada exitosamente!');
        this.showSARConfigModal = false;
        this.loadSARConfig();
        this.loadVentas();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error SAR', 'Error al guardar configuración SAR: ' + (err.error?.mensaje || err.message));
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
    if (this.facturaForm.tipoDocumento === 'Cotizacion') {
      const conCot = this.ventas.find(v => v.esCotizacion && v.numeroCotizacion);
      return conCot?.numeroCotizacion || 'COT-000000';
    }
    if (this.facturaEmitidaData?.numeroFactura) {
      return this.facturaEmitidaData.numeroFactura;
    }
    const conFactura = this.ventas.find(v => v.numeroFactura);
    return conFactura?.numeroFactura || '000-001-01-00000000';
  }

  get nuevaFacturaDisplay(): string {
    if (this.facturaForm.tipoDocumento === 'Cotizacion') {
      return this.facturaEmitida && this.facturaEmitidaData?.numeroCotizacion
        ? this.facturaEmitidaData.numeroCotizacion
        : 'COT-PROXIMA';
    }
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
      this.notificarAdvertencia('Documento vacío', 'Agrega al menos un producto al documento.');
      return;
    }

    this.generandoFactura = true;
    const isCotizacion = this.facturaForm.tipoDocumento === 'Cotizacion';
    const payload = {
      clienteId: Number(this.facturaForm.clienteId),
      sucursalId: 1,
      usuarioNombre: this.currentUser?.name || 'Usuario',
      metodoPagoId: Number(this.facturaForm.metodoPagoId),
      concretarVenta: !isCotizacion,
      emitirFactura: !isCotizacion,
      esCotizacion: isCotizacion,
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

        if (isCotizacion) {
          this.notificarExito(
            'Cotización Generada',
            `¡Cotización ${ventaCreada.numeroCotizacion || 'COT'} generada exitosamente! Puedes editarla o consultar su historial de cambios desde la pantalla de Ventas.`
          );
        } else {
          // Inmediatamente calcular y actualizar el correlativo para que Nueva Factura muestre el siguiente número
          if (ventaCreada.numeroFactura) {
            const partes = ventaCreada.numeroFactura.split('-');
            if (partes.length >= 4) {
              const prefix = `${partes[0]}-${partes[1]}-${partes[2]}-`;
              const currentNum = parseInt(partes[3], 10) || 0;
              const nextNum = currentNum + 1;
              const nextFormatted = nextNum.toString().padStart(8, '0');
              const nextInvoice = `${prefix}${nextFormatted}`;
              if (this.sarConfig) {
                this.sarConfig.correlativoActual = currentNum;
                this.sarConfig.siguienteFactura = nextInvoice;
              } else {
                this.sarConfig = {
                  id: 1,
                  sucursalId: 1,
                  rtn: '08011990123456',
                  cai: ventaCreada.cai || '',
                  rangoDesde: `${prefix}00000001`,
                  rangoHasta: `${prefix}00005000`,
                  fechaLimiteEmision: new Date().toISOString(),
                  correlativoActual: currentNum,
                  siguienteFactura: nextInvoice,
                  facturasDisponibles: 4999,
                  vigente: true
                };
              }
            }
          }

          this.notificarExito('Factura Fiscal Emitida', `¡Factura Fiscal Nº ${ventaCreada.numeroFactura || 'Generada'} emitida con éxito! Se descontó el inventario y se asignó el correlativo SAR.`);
          this.loadSARConfig();
          this.loadCajaEstado();
        }

        this.loadVentas();
        this.loadInventory();
      },
      error: (err) => {
        this.generandoFactura = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Procesar', (isCotizacion ? 'Error al generar cotización: ' : 'Error al generar la factura: ') + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  nuevaFacturaFormulario() {
    this.facturaEmitida = false;
    this.facturaEmitidaData = null;
    this.loadSARConfig();
    this.loadVentas();
    const firstProd = this.products.find(p => p.nombre?.toLowerCase().includes('camara') || p.nombre?.toLowerCase().includes('magnalock') || p.nombre?.toLowerCase().includes('switch')) || this.products[0];
    this.facturaForm = {
      tipoDocumento: this.facturaForm?.tipoDocumento || 'Factura',
      clienteId: this.clientes[0]?.clienteId || 1,
      fecha: new Date().toISOString().split('T')[0],
      metodoPagoId: 1,
      moneda: 'Lempiras (HNL)',
      notas: '',
      notasPie: 'Gracias por su compra en Sistema Vision Tech. Garantía de 1 año en equipos de seguridad electrónica y redes.',
      mostrarNotas: false,
      mostrarNotasPie: false,
      items: [
        {
          productoId: firstProd?.productoId || 1,
          nombre: firstProd?.nombre || 'Equipo Tecnológico',
          cantidad: 1,
          precio: firstProd?.precioVenta || firstProd?.precio || 1450,
          descuento: 0,
          tipoISV: firstProd?.tipoISV || 'ISV15'
        }
      ]
    };
  }

  // =========================================================================
  // IMPRESIÓN Y EXPORTACIÓN A PDF DE FACTURAS Y COTIZACIONES
  // =========================================================================
  imprimirFacturaEnVivo() {
    this.ejecutarImpresionElemento('invoice-preview-area');
  }

  descargarFacturaEnVivoPDF() {
    const num = this.facturaEmitida
      ? ((this.facturaForm.tipoDocumento === 'Cotizacion' ? this.facturaEmitidaData?.numeroCotizacion : this.facturaEmitidaData?.numeroFactura) || 'documento')
      : (this.nuevaFacturaDisplay || 'documento');
    const filename = `${this.facturaForm.tipoDocumento === 'Cotizacion' ? 'Cotizacion' : 'Factura'}_${num}.pdf`;
    this.generarPDFDesdeElemento('invoice-preview-area', filename);
  }

  imprimirFacturaModal() {
    this.ejecutarImpresionElemento('invoice-modal-print-area');
  }

  descargarFacturaModalPDF() {
    const num = this.facturaSeleccionada?.numeroFactura || ('FAC-' + (this.facturaSeleccionada?.ventaId || '01'));
    this.generarPDFDesdeElemento('invoice-modal-print-area', `Factura_${num}.pdf`);
  }

  imprimirCotizacionModal() {
    this.ejecutarImpresionElemento('cotizacion-modal-print-area');
  }

  descargarCotizacionModalPDF() {
    const num = this.cotizacionSeleccionada?.numeroCotizacion || ('COT-' + (this.cotizacionSeleccionada?.ventaId || '01'));
    this.generarPDFDesdeElemento('cotizacion-modal-print-area', `Cotizacion_${num}.pdf`);
  }

  private ejecutarImpresionElemento(elementId: string) {
    const el = document.getElementById(elementId);
    if (!el) {
      window.print();
      return;
    }

    const printWin = window.open('', '_blank', 'width=850,height=900');
    if (!printWin) {
      window.print();
      return;
    }

    const htmlContent = `
      <!DOCTYPE html>
      <html>
        <head>
          <title>Impresión de Documento - Vision Tech</title>
          <meta charset="utf-8">
          <link href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;600;700;800&family=DM+Mono:wght@400;500;700&display=swap" rel="stylesheet">
          <style>
            * { box-sizing: border-box; font-family: 'Manrope', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif !important; }
            body { margin: 0; padding: 24px; background: #fff; color: #1e293b; font-size: 12px; }
            .preview-brand-row { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #0ea5e9; padding-bottom: 14px; margin-bottom: 16px; }
            .preview-cai-box { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px 14px; margin-bottom: 14px; font-size: 11px; line-height: 1.6; color: #334155; }
            .preview-client-box { background: #fff; border: 1px solid #e5e7eb; border-radius: 8px; padding: 12px 14px; margin-bottom: 14px; }
            .preview-table { width: 100%; border-collapse: collapse; margin-bottom: 14px; font-size: 11.5px; }
            .preview-table th { background: #f8fafc; border-bottom: 1.5px solid #cbd5e1; padding: 8px 6px; text-align: left; font-size: 11px !important; font-weight: 700 !important; color: #475569 !important; }
            .preview-table td { padding: 8px 6px; border-bottom: 1px solid #f1f5f9; font-size: 11.5px; }
            .preview-summary { margin-left: auto; width: 250px; display: grid; gap: 5px; border-top: 1px solid #cbd5e1; padding-top: 10px; }
            .preview-summary-row { display: flex; justify-content: space-between; font-size: 11.5px; color: #475569; }
            .preview-summary-total { display: flex; justify-content: space-between; font-size: 14px; font-weight: 800; border-top: 2px solid #0f172a; padding-top: 6px; margin-top: 4px; color: #0f172a; }
            .preview-footer-words { background: #f8fafc; border: 1px dashed #cbd5e1; padding: 10px 14px; border-radius: 8px; margin-top: 14px; font-size: 11px; }
            .badge-cotizacion { background: #f3e8ff; color: #7e22ce; border: 1px solid #e9d5ff; padding: 3px 7px; border-radius: 4px; font-size: 10px; font-weight: 700; display: inline-block; }
            @page { margin: 12mm; size: auto; }
          </style>
        </head>
        <body>
          <div>${el.innerHTML}</div>
          <script>
            window.onload = function() {
              window.focus();
              window.print();
              setTimeout(function() { window.close(); }, 800);
            };
          </script>
        </body>
      </html>
    `;

    printWin.document.open();
    printWin.document.write(htmlContent);
    printWin.document.close();
  }

  private async generarPDFDesdeElemento(elementId: string, nombreArchivo: string) {
    const el = document.getElementById(elementId);
    if (!el) {
      this.notificarAdvertencia('Error', 'No se encontró el elemento a exportar.');
      return;
    }

    try {
      this.notificarInfo('Generando PDF', 'Preparando documento con calidad de impresión...');

      const canvas = await html2canvas(el, {
        scale: 2,
        useCORS: true,
        logging: false,
        backgroundColor: '#ffffff'
      });

      const imgData = canvas.toDataURL('image/png');
      const pdf = new jsPDF('p', 'mm', 'a4');
      const pdfWidth = pdf.internal.pageSize.getWidth();
      const pdfHeight = (canvas.height * pdfWidth) / canvas.width;

      pdf.addImage(imgData, 'PNG', 0, 0, pdfWidth, pdfHeight);
      pdf.save(nombreArchivo);

      Swal.close();
      this.notificarExito('PDF Descargado', `Se descargó "${nombreArchivo}" exitosamente.`);
    } catch (err: any) {
      Swal.close();
      this.notificarError('Error al crear PDF', 'No se pudo generar el archivo PDF: ' + (err?.message || 'Error inesperado'));
    }
  }

  resetearNuevaFactura() {
    this.facturaEmitida = false;
    this.facturaEmitidaData = null;
    this.nuevaFacturaFormulario();
    this.subTabFacturas = 'crear';
  }

  generarFacturaFiscal() {
    this.generarFacturaEnVivo();
  }

  // =========================================================================
  // GESTIÓN Y EDICIÓN DE COTIZACIONES + HISTORIAL DE AUDITORÍA
  // =========================================================================
  abrirModalEditarCotizacion(venta: any) {
    if (!venta.esCotizacion) {
      this.notificarAdvertencia('Normativa SAR', 'Las facturas fiscales emitidas no pueden ser editadas por normativa SAR. Solo las cotizaciones admiten modificaciones.');
      return;
    }
    if (venta.estadoVenta === 'Anulada') {
      this.notificarAdvertencia('Cotización Anulada', 'No se puede editar una cotización que ha sido anulada.');
      return;
    }

    this.cotizacionEditando = venta;
    const itemsEditados: FacturaItemRow[] = (venta.detalles || []).map((d: any) => ({
      productoId: d.productoId,
      nombre: d.producto || '',
      cantidad: d.cantidad,
      precio: d.precio,
      descuento: 0,
      tipoISV: 'ISV15'
    }));

    if (itemsEditados.length === 0 && this.products.length > 0) {
      const p = this.products[0];
      itemsEditados.push({
        productoId: p.productoId || 1,
        nombre: p.nombre || '',
        cantidad: 1,
        precio: p.precioVenta || p.precio || 0,
        descuento: 0,
        tipoISV: p.tipoISV || 'ISV15'
      });
    }

    this.editarCotizacionForm = {
      clienteId: venta.clienteId,
      motivo: '',
      items: itemsEditados
    };
    this.showEditarCotizacionModal = true;
  }

  agregarItemEditarCotizacion() {
    const p = this.products[0];
    this.editarCotizacionForm.items.push({
      productoId: p?.productoId || 1,
      nombre: p?.nombre || '',
      cantidad: 1,
      precio: p?.precioVenta || p?.precio || 0,
      descuento: 0,
      tipoISV: p?.tipoISV || 'ISV15'
    });
  }

  eliminarItemEditarCotizacion(index: number) {
    if (this.editarCotizacionForm.items.length > 1) {
      this.editarCotizacionForm.items.splice(index, 1);
    }
  }

  onProductoChangeEditar(item: FacturaItemRow) {
    const p = this.products.find(x => x.productoId == item.productoId);
    if (p) {
      item.nombre = p.nombre || '';
      item.precio = p.precioVenta || p.precio || 0;
      item.tipoISV = p.tipoISV || 'ISV15';
    }
  }

  calcularLineaTotalEditar(item: FacturaItemRow): number {
    const sub = (item.precio || 0) * (item.cantidad || 0);
    return Math.max(0, sub - (item.descuento || 0));
  }

  get totalEditarCotizacion(): number {
    return this.editarCotizacionForm.items.reduce((acc, i) => acc + this.calcularLineaTotalEditar(i), 0);
  }

  guardarEdicionCotizacion() {
    if (!this.cotizacionEditando) return;
    if (this.editarCotizacionForm.items.length === 0) {
      this.notificarAdvertencia('Cotización vacía', 'Debe incluir al menos un producto.');
      return;
    }

    this.guardandoEdicionCotizacion = true;
    const payload = {
      clienteId: Number(this.editarCotizacionForm.clienteId),
      motivo: 'Ajuste de productos o precios en cotización',
      detalles: this.editarCotizacionForm.items.map(i => ({
        productoId: Number(i.productoId),
        cantidad: Number(i.cantidad),
        precioUnitario: Number(i.precio),
        descuento: Number(i.descuento || 0)
      }))
    };

    this.http.put<any>(`${this.api}/Ventas/${this.cotizacionEditando.ventaId}/Cotizacion`, payload, this.getHeaders()).subscribe({
      next: (ventaActualizada) => {
        this.guardandoEdicionCotizacion = false;
        this.showEditarCotizacionModal = false;
        this.notificarExito(
          'Cotización Actualizada',
          `¡Cotización ${ventaActualizada.numeroCotizacion || this.cotizacionEditando.numeroCotizacion} actualizada correctamente!`
        );
        this.loadVentas();
      },
      error: (err) => {
        this.guardandoEdicionCotizacion = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Modificar', 'Error al modificar cotización: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  abrirHistorialCotizacion(venta: any) {
    this.cotizacionHistorialVenta = venta;
    this.cargandoHistorial = true;
    this.historialCotizacionList = [];
    this.showHistorialCotizacionModal = true;

    this.http.get<any[]>(`${this.api}/Ventas/${venta.ventaId}/Cotizacion/Historial`, this.getHeaders()).subscribe({
      next: (logs) => {
        this.historialCotizacionList = logs || [];
        this.cargandoHistorial = false;
      },
      error: (err) => {
        this.cargandoHistorial = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Cargar Historial', 'No se pudo obtener el historial de cambios: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  async facturarCotizacionDesdeVentas(venta: any) {
    const { value: formValues } = await Swal.fire({
      title: 'Facturar Cotización (SAR)',
      html: `
        <div style="text-align:left; font-size:12px; font-family:'Manrope',sans-serif; color:#334155;">
          <p style="margin-bottom:12px;">Se convertirá la cotización <b>${venta.numeroCotizacion || ('COT-' + venta.ventaId)}</b> a <b>Factura Fiscal Real</b> con CAI y correlativo oficial del SAR. Se rebajará el stock del inventario.</p>
          <div style="background:#f8fafc; padding:10px; border-radius:6px; margin-bottom:12px; border:1px solid #e2e8f0;">
            <div><b>Cliente:</b> ${venta.cliente}</div>
            <div><b>Total a Facturar:</b> L. ${(venta.total || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</div>
          </div>
          <label style="font-weight:700; display:block; margin-bottom:4px;">Método de Pago:</label>
          <select id="swal-metodo-pago" class="swal2-input" style="width:100%; margin:0; font-size:12px; height:38px; border-radius:6px; border:1px solid #cbd5e1;">
            <option value="1">Efectivo</option>
            <option value="2">Tarjeta de Crédito / Débito</option>
            <option value="3">Transferencia Bancaria</option>
          </select>
        </div>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#1f8a67',
      cancelButtonColor: '#64748b',
      confirmButtonText: 'Sí, emitir factura SAR',
      cancelButtonText: 'Cancelar',
      preConfirm: () => {
        const sel = document.getElementById('swal-metodo-pago') as HTMLSelectElement;
        return {
          metodoPagoId: sel ? Number(sel.value) : 1
        };
      }
    });

    if (!formValues) return;

    this.http.post<any>(`${this.api}/Ventas/${venta.ventaId}/FacturarCotizacion?sucursalId=1&metodoPagoId=${formValues.metodoPagoId}`, {}, this.getHeaders()).subscribe({
      next: (factura) => {
        if (factura.numeroFactura) {
          const partes = factura.numeroFactura.split('-');
          if (partes.length >= 4) {
            const prefix = `${partes[0]}-${partes[1]}-${partes[2]}-`;
            const currentNum = parseInt(partes[3], 10) || 0;
            const nextNum = currentNum + 1;
            const nextFormatted = nextNum.toString().padStart(8, '0');
            const nextInvoice = `${prefix}${nextFormatted}`;
            if (this.sarConfig) {
              this.sarConfig.correlativoActual = currentNum;
              this.sarConfig.siguienteFactura = nextInvoice;
            }
          }
        }
        this.notificarExito('Factura SAR Emitida', `¡Cotización convertida exitosamente a Factura Fiscal Nº ${factura.numeroFactura || ''}! Se descontó el inventario.`);
        this.loadVentas();
        this.loadInventory();
        this.loadSARConfig();
        this.verFactura(factura);
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Facturar', 'Error al facturar cotización: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  verCotizacion(venta: any) {
    this.cotizacionSeleccionada = venta;
    this.showVerCotizacionModal = true;
  }

  // =========================================================================
  // GESTIÓN DE COMPRAS Y PROVEEDORES
  // =========================================================================
  abrirModalNuevaCompra() {
    if (this.proveedores.length === 0) {
      this.notificarAdvertencia('Sin proveedores', 'Debes registrar al menos un proveedor antes de crear una orden de compra.');
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
      this.notificarAdvertencia('Orden vacía', 'Agrega al menos un producto a la compra.');
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
        this.notificarExito('Orden de Compra', `¡Orden de Compra #${compraCreada.compraId} registrada exitosamente!`);
        this.loadCompras();
      },
      error: (err) => {
        this.guardandoCompra = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error en Compra', 'Error al registrar compra: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  async recibirCompra(compraId: number) {
    const confirmado = await this.confirmarAccion(
      '¿Recibir Mercancía?',
      '¿Deseas marcar esta orden como RECIBIDA? El inventario se actualizará automáticamente con las unidades ingresadas.',
      'Sí, recibir e ingresar'
    );
    if (!confirmado) return;

    this.http.put<any>(`${this.api}/Compras/${compraId}/Recibir`, {}, this.getHeaders()).subscribe({
      next: () => {
        this.notificarExito('Mercancía Recibida', '¡Mercancía recibida e inventario actualizado exitosamente!');
        this.loadCompras();
        this.loadInventory();
        this.loadProducts();
        this.loadMovimientos();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Recibir', 'Error al recibir compra: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  async anularCompra(compraId: number) {
    const confirmado = await this.confirmarAccion(
      '¿Anular Orden de Compra?',
      '¿Estás seguro de anular esta orden de compra?',
      'Sí, anular',
      'Cancelar',
      true
    );
    if (!confirmado) return;

    this.http.put<any>(`${this.api}/Compras/${compraId}/Anular`, {}, this.getHeaders()).subscribe({
      next: () => {
        this.notificarExito('Orden Anulada', 'Orden de compra anulada.');
        this.loadCompras();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Anular', 'Error al anular compra: ' + (err.error?.mensaje || err.message));
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
      this.notificarAdvertencia('Campo requerido', 'El nombre del proveedor es obligatorio.');
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
        this.notificarExito(
          this.proveedorEditandoId ? 'Proveedor Actualizado' : 'Proveedor Registrado',
          this.proveedorEditandoId ? 'Proveedor actualizado con éxito.' : '¡Proveedor creado exitosamente!'
        );
        this.loadProveedores();
      },
      error: (err) => {
        this.guardandoProveedor = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error de Proveedor', 'Error al guardar proveedor: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  async eliminarProveedor(id: number) {
    const confirmado = await this.confirmarAccion(
      '¿Eliminar Proveedor?',
      '¿Estás seguro de eliminar este proveedor?',
      'Sí, eliminar',
      'Cancelar',
      true
    );
    if (!confirmado) return;

    this.http.delete<any>(`${this.api}/Proveedores/${id}`, this.getHeaders()).subscribe({
      next: () => {
        this.notificarExito('Eliminado', 'Proveedor eliminado.');
        this.loadProveedores();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Eliminar', 'Error al eliminar proveedor: ' + (err.error?.mensaje || err.message));
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
        this.notificarExito('Caja Abierta', `¡Caja abierta exitosamente con fondo inicial de L. ${res.montoApertura}!`);
        this.showAbrirCajaModal = false;
        this.loadCajaEstado();
        this.loadCajaHistorial();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error de Caja', 'Error al abrir caja: ' + (err.error?.mensaje || err.message));
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
      this.notificarAdvertencia('Caja no encontrada', 'No se detectó el identificador de la caja actual.');
      return;
    }

    const payload = {
      montoEfectivoFinal: Number(this.montoCierreInput)
    };

    this.http.post<any>(`${this.api}/Caja/${this.estadoCaja.id}/cerrar`, payload, this.getHeaders()).subscribe({
      next: (res) => {
        const dif = res.diferencia ?? 0;
        const msgDif = dif === 0 ? '¡Arqueo exacto!' : dif > 0 ? `Sobrante: L. ${dif}` : `Faltante: L. ${dif}`;
        this.notificarExito('Caja Cerrada', `¡Caja cerrada exitosamente! ${msgDif}`);
        this.showCerrarCajaModal = false;
        this.loadCajaEstado();
        this.loadCajaHistorial();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error de Cierre', 'Error al cerrar caja: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  // =========================================================================
  // ACCIONES EN VENTAS (CONFIRMAR, ANULAR)
  // =========================================================================
  async confirmarVenta(ventaId: number) {
    const confirmado = await this.confirmarAccion(
      '¿Confirmar Venta?',
      '¿Deseas confirmar esta venta?',
      'Sí, confirmar'
    );
    if (!confirmado) return;

    this.http.put<any>(`${this.api}/Ventas/${ventaId}/Confirmar`, {}, this.getHeaders()).subscribe({
      next: () => {
        this.notificarExito('Venta Confirmada', 'Venta confirmada exitosamente.');
        this.loadVentas();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Confirmar', 'Error al confirmar venta: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  async anularVenta(ventaId: number) {
    const confirmado = await this.confirmarAccion(
      '¿Anular Venta?',
      '¿Estás seguro de anular esta venta? El stock de los productos volverá al inventario.',
      'Sí, anular venta',
      'Cancelar',
      true
    );
    if (!confirmado) return;

    this.http.put<any>(`${this.api}/Ventas/${ventaId}/Anular`, {}, this.getHeaders()).subscribe({
      next: () => {
        this.notificarExito('Venta Anulada', 'Venta anulada.');
        this.loadVentas();
        this.loadInventory();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Anular', 'Error al anular venta: ' + (err.error?.mensaje || err.message));
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
      this.notificarAdvertencia('Campos incompletos', 'Todos los campos son obligatorios.');
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
        this.notificarExito('Usuario Creado', '¡Usuario creado exitosamente!');
        this.loadUsuarios();
      },
      error: (err) => {
        this.guardandoUsuario = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Crear Usuario', 'Error al crear usuario: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  async eliminarUsuario(id: number) {
    const confirmado = await this.confirmarAccion(
      '¿Eliminar Usuario?',
      '¿Estás seguro de eliminar este usuario?',
      'Sí, eliminar',
      'Cancelar',
      true
    );
    if (!confirmado) return;

    this.http.delete<any>(`${this.api}/Acceso/Usuarios/${id}`, this.getHeaders()).subscribe({
      next: () => {
        this.notificarExito('Eliminado', 'Usuario eliminado.');
        this.loadUsuarios();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Eliminar', 'Error al eliminar usuario: ' + (err.error?.mensaje || err.message));
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
        this.notificarExito('Producto Actualizado', 'Producto actualizado con éxito.');
        this.loadProducts();
      },
      error: (err) => {
        this.guardandoEditarProducto = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Actualizar', 'Error al actualizar producto: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  async eliminarProducto(id: number) {
    const confirmado = await this.confirmarAccion(
      '¿Eliminar Producto?',
      '¿Estás seguro de eliminar este producto del catálogo?',
      'Sí, eliminar',
      'Cancelar',
      true
    );
    if (!confirmado) return;

    this.http.delete<any>(`${this.api}/Productos/${id}`, this.getHeaders()).subscribe({
      next: () => {
        this.notificarExito('Eliminado', 'Producto eliminado.');
        this.loadProducts();
        this.loadInventory();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Eliminar', 'Error al eliminar producto: ' + (err.error?.mensaje || err.message));
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
        this.notificarExito('Cliente Actualizado', 'Cliente actualizado con éxito.');
        this.loadClientes();
      },
      error: (err) => {
        this.guardandoEditarCliente = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Actualizar', 'Error al actualizar cliente: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  async eliminarCliente(id: number) {
    const confirmado = await this.confirmarAccion(
      '¿Eliminar Cliente?',
      '¿Estás seguro de eliminar este cliente?',
      'Sí, eliminar',
      'Cancelar',
      true
    );
    if (!confirmado) return;

    this.http.delete<any>(`${this.api}/Clientes/${id}`, this.getHeaders()).subscribe({
      next: () => {
        this.notificarExito('Eliminado', 'Cliente eliminado.');
        this.loadClientes();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Eliminar', 'Error al eliminar cliente: ' + (err.error?.mensaje || err.message));
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
      this.notificarAdvertencia('Campo requerido', 'El nombre del cliente es obligatorio.');
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
        this.notificarExito('Cliente Registrado', `¡Cliente "${clienteCreado.nombre}" registrado exitosamente!`);
        this.showNuevoClienteModal = false;
        this.loadClientes();
        if (clienteCreado.clienteId) {
          this.facturaForm.clienteId = clienteCreado.clienteId;
          this.nuevaVenta.clienteId = clienteCreado.clienteId;
        }
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Registrar', 'Error al registrar cliente: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  abrirModalMovimientoInventario(productoId?: number) {
    if (productoId) {
      this.movimientoInventario.productoId = productoId;
    } else if (this.products.length > 0) {
      this.movimientoInventario.productoId = this.products[0].productoId!;
    }
    this.movimientoInventario.tipoMovimiento = 'ENTRADA';
    this.movimientoInventario.cantidad = 10;
    this.showMovimientoInventarioModal = true;
  }

  guardarMovimientoInventario() {
    if (this.movimientoInventario.cantidad <= 0) {
      this.notificarAdvertencia('Cantidad inválida', 'La cantidad a mover debe ser mayor a 0.');
      return;
    }

    const payload = {
      productoId: Number(this.movimientoInventario.productoId),
      tipoMovimiento: this.movimientoInventario.tipoMovimiento,
      cantidad: Number(this.movimientoInventario.cantidad)
    };

    this.http.post<any>(`${this.api}/Inventario/Movimiento`, payload, this.getHeaders()).subscribe({
      next: () => {
        this.notificarExito('Inventario Actualizado', '¡Movimiento de inventario registrado con éxito!');
        this.showMovimientoInventarioModal = false;
        this.loadInventory();
        this.loadProducts();
        this.loadMovimientos();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Registrar', 'Error al registrar movimiento: ' + (err.error?.mensaje || err.message));
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
      this.notificarAdvertencia('Campo requerido', 'El nombre del producto es obligatorio.');
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
        this.notificarExito(
          'Producto Creado',
          `¡Producto "${prodCreado.nombre}" creado exitosamente con stock de ${payload.stockInicial} unidades! Ya está disponible para la venta.`
        );
        this.loadProducts();
        this.loadInventory();
        this.loadMovimientos();
      },
      error: (err) => {
        this.guardandoProducto = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Guardar', 'Error al registrar producto: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  abrirSARConfig() {
    if (!this.isAdmin) {
      this.notificarAdvertencia('Acceso restringido', 'Acceso restringido: Solo administradores pueden configurar los datos del SAR.');
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
          this.notificarInfo('Sin Factura', 'Esta venta aún no tiene una factura fiscal emitida.');
        }
      }
    });
  }

  emitirFactura(ventaId: number) {
    this.http.post<any>(`${this.api}/Ventas/${ventaId}/Facturar?sucursalId=1`, {}, this.getHeaders()).subscribe({
      next: (factura) => {
        if (factura.numeroFactura) {
          const partes = factura.numeroFactura.split('-');
          if (partes.length >= 4) {
            const prefix = `${partes[0]}-${partes[1]}-${partes[2]}-`;
            const currentNum = parseInt(partes[3], 10) || 0;
            const nextNum = currentNum + 1;
            const nextFormatted = nextNum.toString().padStart(8, '0');
            const nextInvoice = `${prefix}${nextFormatted}`;
            if (this.sarConfig) {
              this.sarConfig.correlativoActual = currentNum;
              this.sarConfig.siguienteFactura = nextInvoice;
            }
          }
        }
        this.notificarExito('Factura Emitida', `¡Factura emitida correctamente! Nº: ${factura.numeroFactura}`);
        this.loadVentas();
        this.loadSARConfig();
        this.verFactura({ ventaId, ...factura });
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Emitir', 'Error al emitir factura: ' + (err.error?.mensaje || err.message));
        }
      }
    });
  }

  iconFor(name: string) {
    return this.icons[name as keyof typeof this.icons];
  }

  getProductoPorId(productoId?: number): Product | undefined {
    if (!productoId) return undefined;
    return this.products.find(p => p.productoId === productoId);
  }

  get filteredInventory(): Inventory[] {
    if (!this.search.trim()) return this.inventory;
    const q = this.search.toLowerCase().trim();
    return this.inventory.filter(i => {
      const prod = this.getProductoPorId(i.productoId);
      return (i.nombreProducto || '').toLowerCase().includes(q) ||
             (prod?.codigo || '').toLowerCase().includes(q) ||
             (prod?.codigoBarras || '').toLowerCase().includes(q);
    });
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

  // =========================================================================
  // ALERTAS PREVENTIVAS DE CAI Y RANGO SAR
  // =========================================================================
  get alertaCAI(): { tipo: 'danger' | 'warning'; titulo: string; mensaje: string; detalle: string; diasRestantes: number; facturasDisponibles: number } | null {
    if (!this.sarConfig) return null;

    const hoy = new Date();
    const fechaLimite = new Date(this.sarConfig.fechaLimiteEmision);
    const diffTime = fechaLimite.getTime() - hoy.getTime();
    const diasRestantes = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    let facturasDisponibles = this.sarConfig.facturasDisponibles;
    if (facturasDisponibles === undefined || facturasDisponibles === null) {
      const hastaNum = this.extraerNumeroCorrelativo(this.sarConfig.rangoHasta);
      facturasDisponibles = Math.max(0, hastaNum - (this.sarConfig.correlativoActual || 0));
    }

    if (diasRestantes <= 0) {
      return {
        tipo: 'danger',
        titulo: '¡CAI VENCIDO! FECHA LÍMITE DE EMISIÓN EXPIRADA',
        mensaje: `El CAI registrado expiró el ${fechaLimite.toLocaleDateString('es-HN')}. Por normativa SAR, no se permite emitir facturas legales hasta registrar un nuevo CAI.`,
        detalle: `Actualice inmediatamente la autorización del CAI en Configuración SAR.`,
        diasRestantes,
        facturasDisponibles
      };
    }

    if (facturasDisponibles <= 0) {
      return {
        tipo: 'danger',
        titulo: '¡RANGO DE FACTURAS SAR AGOTADO!',
        mensaje: `Se ha alcanzado el límite de numeración autorizada (${this.sarConfig.rangoHasta}).`,
        detalle: `Debe tramitar un nuevo rango ante el SAR y registrarlo en el sistema.`,
        diasRestantes,
        facturasDisponibles
      };
    }

    if (diasRestantes <= 30 && facturasDisponibles <= 100) {
      return {
        tipo: 'danger',
        titulo: `¡ALERTA CRÍTICA SAR! CAI VENCE EN ${diasRestantes} DÍAS Y QUEDAN ${facturasDisponibles} FACTURAS`,
        mensaje: `Fecha límite: ${fechaLimite.toLocaleDateString('es-HN')} | Quedan solo ${facturasDisponibles} comprobantes autorizados.`,
        detalle: `Inicie el trámite de renovación de CAI y asignación de rango ante el SAR de inmediato.`,
        diasRestantes,
        facturasDisponibles
      };
    }

    if (diasRestantes <= 30) {
      return {
        tipo: 'warning',
        titulo: `¡ALERTA PREVENTIVA CAI! VENCE EN ${diasRestantes} DÍAS`,
        mensaje: `La autorización fiscal del SAR vencerá el ${fechaLimite.toLocaleDateString('es-HN')}.`,
        detalle: `Quedan ${diasRestantes} días calendario para gestionar la renovación del CAI con el SAR antes de su bloqueo.`,
        diasRestantes,
        facturasDisponibles
      };
    }

    if (facturasDisponibles <= 100) {
      return {
        tipo: 'warning',
        titulo: `¡ALERTA PREVENTIVA CAI! QUEDAN SOLO ${facturasDisponibles} FACTURAS DISPONIBLES`,
        mensaje: `El rango autorizado está próximo a agotarse. Siguiente factura: ${this.sarConfig.siguienteFactura}. Límite: ${this.sarConfig.rangoHasta}.`,
        detalle: `Gestione con anticipación la ampliación de rango de facturación ante el SAR.`,
        diasRestantes,
        facturasDisponibles
      };
    }

    return null;
  }

  extraerNumeroCorrelativo(rango: string): number {
    if (!rango) return 0;
    const partes = rango.split('-');
    if (partes.length >= 4) {
      return parseInt(partes[3], 10) || 0;
    }
    return 0;
  }

  // =========================================================================
  // MÓDULO DE REPORTES FISCALES Y GERENCIALES (EXCEL Y PDF)
  // =========================================================================
  subTabReporte: 'ventas-sar' | 'cierre-caja' | 'inventario' | 'auditoria' = 'ventas-sar';
  reporteMesVentas: number = new Date().getMonth() + 1;
  reporteAnoVentas: number = new Date().getFullYear();
  cierreSeleccionadoReporteId: number = 0;

  get ventasFiltradasSAR(): VentaItem[] {
    return this.ventas.filter(v => {
      if (v.esCotizacion || !v.numeroFactura) return false;
      const f = new Date(v.fechaFactura || v.fechaVenta);
      return (f.getMonth() + 1) === Number(this.reporteMesVentas) && f.getFullYear() === Number(this.reporteAnoVentas);
    });
  }

  get totalVentasSARMes(): number {
    return this.ventasFiltradasSAR.reduce((acc, v) => acc + (v.total || 0), 0);
  }

  get totalISV15SARMes(): number {
    return this.ventasFiltradasSAR.reduce((acc, v) => acc + (v.isv15 || ((v.total || 0) - ((v.total || 0) / 1.15))), 0);
  }

  get totalGravado15SARMes(): number {
    return this.ventasFiltradasSAR.reduce((acc, v) => acc + (v.montoGravado15 || ((v.total || 0) / 1.15)), 0);
  }



  actualizarPasswordSeguridad() {
    if (!this.seguridadPassword.nuevaPassword || !this.seguridadPassword.confirmarPassword) {
      this.notificarAdvertencia('Campos Requeridos', 'Por favor ingresa y confirma la nueva contraseña.');
      return;
    }
    if (this.seguridadPassword.nuevaPassword !== this.seguridadPassword.confirmarPassword) {
      this.notificarAdvertencia('Contraseñas no coinciden', 'La confirmación no coincide con la nueva contraseña.');
      return;
    }
    if (this.seguridadPassword.nuevaPassword.length < 4) {
      this.notificarAdvertencia('Contraseña muy corta', 'La contraseña debe tener al menos 4 caracteres.');
      return;
    }

    // Buscar el ID del usuario actual en la lista o usar 1
    const usuarioActual = this.usuarios.find(u => u.email?.toLowerCase() === this.currentUser.email?.toLowerCase()) || this.usuarios[0] || { usuarioId: 1 };
    const usuarioId = usuarioActual.usuarioId || 1;

    this.guardandoPassword = true;
    const payload = {
      nombre: this.currentUser.name || 'Angel Guillen',
      email: this.currentUser.email || 'admin@visiontech.com',
      contraseña: this.seguridadPassword.nuevaPassword,
      perfilId: usuarioActual.perfilId || 1
    };

    this.http.put(`${this.api}/Acceso/Usuarios/${usuarioId}`, payload, this.getHeaders()).subscribe({
      next: () => {
        this.guardandoPassword = false;
        this.seguridadPassword = { passwordActual: '', nuevaPassword: '', confirmarPassword: '' };
        this.notificarExito('Contraseña Actualizada', 'Tu contraseña ha sido actualizada y encriptada correctamente. Podrás iniciar sesión con tu nueva clave.');
      },
      error: (err) => {
        this.guardandoPassword = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Actualizar Contraseña', err.error?.mensaje || 'No se pudo actualizar la contraseña.');
        }
      }
    });
  }

  loadAuditoriaLogs() {
    if (!this.isAdmin) return;
    this.cargandoAuditoria = true;
    this.http.get<any[]>(`${this.api}/Auditoria`, this.getHeaders()).subscribe({
      next: (data) => {
        this.logsAuditoriaList = Array.isArray(data) ? data : [];
        this.cargandoAuditoria = false;
      },
      error: (err) => {
        this.cargandoAuditoria = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error en Bitácora', err.error?.mensaje || 'No se pudo cargar la bitácora de auditoría.');
        }
      }
    });
  }

    guardarConfiguracionEmpresa() {
    this.guardandoConfiguracion = true;
    localStorage.setItem('vt_company_nombre', this.companyConfig.nombre);
    localStorage.setItem('vt_company_gerente_nombre', this.companyConfig.nombreGerente);
    localStorage.setItem('vt_company_gerente_apellido', this.companyConfig.apellidoGerente);
    localStorage.setItem('vt_company_rtn', this.companyConfig.rtn);
    localStorage.setItem('vt_company_direccion', this.companyConfig.direccion);
    localStorage.setItem('vt_company_telefono', this.companyConfig.telefono);
    localStorage.setItem('vt_company_email', this.companyConfig.email);
    localStorage.setItem('vt_company_nota_doc', this.companyConfig.notaDocumento);
    localStorage.setItem('vt_company_nota_pie', this.companyConfig.notaPie);
    localStorage.setItem('vt_company_logo', this.companyConfig.logoUrl);

    // Actualizar notas predeterminadas de facturación
    this.facturaForm.notas = this.companyConfig.notaDocumento;
    this.facturaForm.notasPie = this.companyConfig.notaPie;

    setTimeout(() => {
      this.guardandoConfiguracion = false;
      this.notificarExito('Configuración Guardada', 'Los datos generales y notas predeterminadas se han actualizado correctamente.');
    }, 400);
  }

  onLogoUpload(event: any) {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.companyConfig.logoUrl = e.target.result;
      };
      reader.readAsDataURL(file);
    }
  }

  abrirHistorialProducto(prod: Product) {
    this.productoHistorialSeleccionado = prod;
    this.showHistorialProductoModal = true;
    this.cargandoHistorialProducto = true;
    this.historialProductoList = [];

    this.http.get<any[]>(`${this.api}/Productos/${prod.productoId}/Historial`, this.getHeaders()).subscribe({
      next: (data) => {
        this.historialProductoList = Array.isArray(data) ? data : [];
        this.cargandoHistorialProducto = false;
      },
      error: (err) => {
        this.cargandoHistorialProducto = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Cargar Historial', err.error?.mensaje || 'No se pudo obtener la bitácora de este producto.');
        }
      }
    });
  }

  exportarVentasSARExcel() {
    const data = this.ventasFiltradasSAR.map((v, idx) => ({
      'No.': idx + 1,
      'Fecha Emisión': new Date(v.fechaFactura || v.fechaVenta).toLocaleDateString('es-HN'),
      'No. Factura Fiscal': v.numeroFactura || '—',
      'CAI': v.cai || '—',
      'RTN Cliente': v.clienteRtn || '00000000000000',
      'Nombre Cliente': v.cliente,
      'Exento (L.)': Number((v.montoExento || 0).toFixed(2)),
      'Gravado 15% (L.)': Number((v.montoGravado15 || ((v.total || 0) / 1.15)).toFixed(2)),
      'Gravado 18% (L.)': Number((v.montoGravado18 || 0).toFixed(2)),
      'ISV 15% (L.)': Number((v.isv15 || ((v.total || 0) - ((v.total || 0) / 1.15))).toFixed(2)),
      'ISV 18% (L.)': Number((v.isv18 || 0).toFixed(2)),
      'Total Facturado (L.)': Number((v.total || 0).toFixed(2)),
      'Estado': v.estadoVenta
    }));

    if (data.length === 0) {
      this.notificarAdvertencia('Sin datos', 'No hay facturas fiscales emitidas para el mes seleccionado.');
      return;
    }

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Libro_Ventas_SAR');
    XLSX.writeFile(wb, `Libro_Ventas_SAR_${this.reporteMesVentas}_${this.reporteAnoVentas}.xlsx`);
    this.notificarExito('Reporte Exportado', 'El reporte mensual de ventas para el SAR se ha descargado en formato Excel.');
  }

  imprimirVentasSAR() {
    window.print();
  }

  // Reporte Valorización de Inventario
  get inventarioValorizado(): any[] {
    return this.inventory.map(inv => {
      const prod = this.products.find(p => p.productoId === inv.productoId);
      const stock = inv.stock || 0;
      const precioVenta = prod?.precioVenta || prod?.precio || 0;
      const costoEstimado = precioVenta * 0.65; // Margen referencial 35%
      const totalCosto = stock * costoEstimado;
      const totalVenta = stock * precioVenta;
      const margen = totalVenta - totalCosto;

      return {
        productoId: inv.productoId,
        codigo: prod?.codigo || prod?.codigoBarras || `SEC-${inv.productoId}`,
        nombre: prod?.nombre || inv.nombreProducto || 'Equipo de Seguridad / Redes',
        stock: stock,
        costoEstimado: costoEstimado,
        precioVenta: precioVenta,
        totalCosto: totalCosto,
        totalVenta: totalVenta,
        margenProyectado: margen
      };
    });
  }

  get totalStockUnidades(): number {
    return this.inventarioValorizado.reduce((acc, i) => acc + i.stock, 0);
  }

  get totalValorizadoCosto(): number {
    return this.inventarioValorizado.reduce((acc, i) => acc + i.totalCosto, 0);
  }

  get totalValorizadoVenta(): number {
    return this.inventarioValorizado.reduce((acc, i) => acc + i.totalVenta, 0);
  }

  exportarInventarioExcel() {
    const data = this.inventarioValorizado.map((i, idx) => ({
      'No.': idx + 1,
      'Código / SKU': i.codigo,
      'Equipo / Dispositivo': i.nombre,
      'Stock Actual': i.stock,
      'Costo Estimado (L.)': Number(i.costoEstimado.toFixed(2)),
      'Precio Venta (L.)': Number(i.precioVenta.toFixed(2)),
      'Valor Total al Costo (L.)': Number(i.totalCosto.toFixed(2)),
      'Valor a Precio Venta (L.)': Number(i.totalVenta.toFixed(2)),
      'Margen Proyectado (L.)': Number(i.margenProyectado.toFixed(2))
    }));

    if (data.length === 0) {
      this.notificarAdvertencia('Sin inventario', 'No hay registros de inventario disponibles para exportar.');
      return;
    }

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Valorizacion_Inventario');
    XLSX.writeFile(wb, `Valorizacion_Inventario_VisionTech_${new Date().toISOString().split('T')[0]}.xlsx`);
    this.notificarExito('Inventario Exportado', 'La valorización de inventario se descargó con éxito en formato Excel.');
  }

  imprimirInventario() {
    window.print();
  }

  // Reporte Corte Z Diario
  get cierreSeleccionadoReporte(): CierreCajaItem | undefined {
    if (this.cierreSeleccionadoReporteId) {
      return this.historialCaja.find(c => c.id === Number(this.cierreSeleccionadoReporteId));
    }
    return this.historialCaja[0];
  }

  imprimirCorteZ() {
    window.print();
  }

  get fechaActualDisplay(): string {
    return new Date().toLocaleDateString('es-HN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }
}
