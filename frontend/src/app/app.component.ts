import { environment } from '../environments/environment';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, inject, HostListener, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';
import * as XLSX from 'xlsx';
import html2canvas from 'html2canvas';
import { jsPDF } from 'jspdf';
import {Bell, Boxes, ChevronDown, CircleDollarSign, ClipboardList,
  FileBarChart, Home, LogOut, Menu, Package, Search,
  Settings, ShoppingCart, Store, Truck, Users, X, FileText, CheckCircle2,
  AlertTriangle, Plus, Lock, Printer, Trash2, Check, Shield, RotateCcw,
  Pencil, History, Ban, Layers, Download, Filter, Eye, EyeOff, Upload, FileSpreadsheet, Copy, LucideAngularModule, ChevronLeft, ChevronRight} from 'lucide-angular';

interface Product {
  productoId?: number;
  nombre?: string;
  codigo?: string;
  codigoBarras?: string;
  descripcion?: string;
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
  clavePlana?: string;
}

interface PerfilItem {
  perfilId: number;
  nombre: string;
  permisos?: string[];
}

interface VentaItem {
  ventaId: number;
  clienteId: number;
  cliente: string;
  clienteRtn?: string;
  rtn?: string;
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
  busqueda?: string;
  mostrarDropdown?: boolean;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  static getFechaHoyHonduras(): string {
    const ahora = new Date();
    return new Intl.DateTimeFormat('en-CA', {
      timeZone: 'America/Tegucigalpa',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    }).format(ahora);
  }

  getFechaHoyHonduras(): string {
    return AppComponent.getFechaHoyHonduras();
  }
  private http = inject(HttpClient);

  readonly icons = {
    ChevronLeft, ChevronRight,
    Bell, Boxes, ChevronDown, CircleDollarSign, ClipboardList,
    FileBarChart, Home, LogOut, Menu, Package, Search,
    Settings, ShoppingCart, Store, Truck, Users, X, FileText, CheckCircle2,
    AlertTriangle, Plus, Lock, Printer, Trash2, Check, Shield, RotateCcw,
    Pencil, History, Ban, Layers, Download, Filter, Eye, EyeOff, Upload, FileSpreadsheet, Copy
  };

  readonly api = environment.apiUrl;
  loginMode = !sessionStorage.getItem('webwave_token');
  loading = false;
  error = '';
  credentials = { email: '', password: '' };
  mostrarPasswordLogin = false;
  active = 'Resumen';
  search = '';
  searchProductos = '';
  paginaActualProductos = 1;
  tamanoPaginaProductos = 10;
  searchClientes = '';
  searchProveedores = '';
  searchCompras = '';
  searchReporteVentas = '';
  searchReporteInventario = '';

  // Filtros por columna para la tabla de Facturas / Cotizaciones
  filtroVentas = {
    id: '',
    tipo: 'TODOS',
    documento: '',
    cliente: '',
    fecha: '',
    cai: 'TODOS',
    montoMin: null as number | null,
    estado: 'TODOS'
  };

  limpiarFiltrosVentas() {
    this.filtroVentas = {
      id: '',
      tipo: 'TODOS',
      documento: '',
      cliente: '',
      fecha: '',
      cai: 'TODOS',
      montoMin: null,
      estado: 'TODOS'
    };
  }

  get hayFiltrosVentasActivos(): boolean {
    return !!(
      this.filtroVentas.id ||
      this.filtroVentas.tipo !== 'TODOS' ||
      this.filtroVentas.documento ||
      this.filtroVentas.cliente ||
      this.filtroVentas.fecha ||
      this.filtroVentas.cai !== 'TODOS' ||
      (this.filtroVentas.montoMin !== null && this.filtroVentas.montoMin !== undefined && this.filtroVentas.montoMin > 0) ||
      this.filtroVentas.estado !== 'TODOS'
    );
  }
  sidebarOpen = false;

  currentUser: {
    name: string;
    email?: string;
    role: string;
    initials: string;
  } = {
    name: sessionStorage.getItem('webwave_user_name') || '',
    email: sessionStorage.getItem('webwave_user_email') || '',
    role: sessionStorage.getItem('webwave_user_role') || '',
    initials: sessionStorage.getItem('webwave_user_initials') || ''
  };

  get isAdmin(): boolean {
    const role = (this.currentUser.role || '').toLowerCase();
    return role.includes('admin') || role === 'administrador' || role.includes('super');
  }

  get isSuperUsuario(): boolean {
    const role = (this.currentUser.role || '').toLowerCase();
    return role.includes('super') || role === 'administrador' || role === 'admin';
  }

  get isFacturador(): boolean {
    const role = (this.currentUser.role || '').toLowerCase();
    return role.includes('factur') || role.includes('venta') || role.includes('vendedor');
  }

  get isCajero(): boolean {
    const role = (this.currentUser.role || '').toLowerCase();
    return role.includes('cajer');
  }

  get isAlmacen(): boolean {
    const role = (this.currentUser.role || '').toLowerCase();
    return role.includes('almacen') || role.includes('inventario');
  }

  todasLasPantallasDisponibles: { label: string; icon: string; descripcion: string }[] = [
    { label: 'Resumen', icon: 'Home', descripcion: 'Panel general y métricas clave en vivo' },
    { label: 'Facturas', icon: 'FileText', descripcion: 'Facturación SAR y Cotizaciones' },
    { label: 'Compras', icon: 'Truck', descripcion: 'Gestión y recepción de órdenes de compra' },
    { label: 'Inventario', icon: 'Boxes', descripcion: 'Control de existencias y kardex físico' },
    { label: 'Productos', icon: 'Package', descripcion: 'Catálogo de productos, precios e ISV' },
    { label: 'Clientes', icon: 'Users', descripcion: 'Directorio de clientes y RTN fiscal' },
    { label: 'Proveedores', icon: 'Store', descripcion: 'Directorio de casas proveedoras' },
    { label: 'Caja', icon: 'CircleDollarSign', descripcion: 'Aperturas, arqueos y cierres de turno' },
    { label: 'Reportes', icon: 'FileBarChart', descripcion: 'Libro de ventas SAR, corte Z y reportes' },
    { label: 'Usuarios', icon: 'Shield', descripcion: 'Administración de usuarios y roles RBAC' },
    { label: 'Configuración', icon: 'Settings', descripcion: 'Parámetros fiscales SAR y del sistema' }
  ];

  getDefaultScreensForRole(roleName?: string): string[] {
    const rol = (roleName || '').toLowerCase();
    if (rol.includes('admin') || rol.includes('super')) {
      return this.todasLasPantallasDisponibles.map(p => p.label);
    }
    if (rol.includes('factura') || rol.includes('venta') || rol.includes('vendedor')) {
      return ['Resumen', 'Facturas', 'Clientes', 'Productos'];
    }
    if (rol.includes('caj')) {
      return ['Resumen', 'Facturas', 'Caja', 'Clientes', 'Productos'];
    }
    if (rol.includes('almacen') || rol.includes('inventario') || rol.includes('bodega')) {
      return ['Resumen', 'Inventario', 'Productos', 'Compras', 'Proveedores'];
    }
    return ['Resumen', 'Facturas', 'Clientes', 'Productos'];
  }

  puedeVerModulo(modulo: string): boolean {
    if (this.isAdmin) return true;

    // Verificar si el usuario tiene pantallas personalizadas asignadas
    const emailKey = (this.currentUser.email || '').toLowerCase();
    const customScreens = localStorage.getItem('vt_user_screens_' + emailKey);
    if (customScreens) {
      try {
        const screens: string[] = JSON.parse(customScreens);
        if (Array.isArray(screens) && screens.length > 0) {
          return screens.includes(modulo);
        }
      } catch (e) {}
    }

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

  getModulosAccesoPorPerfil(perfilId: number): { nombre: string; icon: string; descripcion: string }[] {
    const id = Number(perfilId);
    const perfil = this.perfiles.find(p => p.perfilId === id);
    const nombreRol = (perfil ? perfil.nombre : '').toLowerCase();

    if (id === 1 || nombreRol.includes('admin')) {
      return [
        { nombre: 'Resumen', icon: 'Home', descripcion: 'Panel general y métricas clave' },
        { nombre: 'Facturas', icon: 'FileText', descripcion: 'Facturación SAR y Cotizaciones' },
        { nombre: 'Compras', icon: 'Truck', descripcion: 'Gestión de órdenes de compra' },
        { nombre: 'Inventario', icon: 'Boxes', descripcion: 'Control de existencias y kardex' },
        { nombre: 'Productos', icon: 'Package', descripcion: 'Catálogo de productos y precios' },
        { nombre: 'Clientes', icon: 'Users', descripcion: 'Directorio de clientes y RTN' },
        { nombre: 'Proveedores', icon: 'Store', descripcion: 'Directorio de casas proveedoras' },
        { nombre: 'Caja', icon: 'CircleDollarSign', descripcion: 'Aperturas y cierres de caja' },
        { nombre: 'Reportes', icon: 'FileBarChart', descripcion: 'Libro de ventas SAR, caja y auditoría' },
        { nombre: 'Usuarios', icon: 'Shield', descripcion: 'Administración de usuarios y roles' },
        { nombre: 'Configuración', icon: 'Settings', descripcion: 'Parámetros fiscales y del sistema' }
      ];
    } else if (nombreRol.includes('factura') || nombreRol.includes('venta')) {
      return [
        { nombre: 'Resumen', icon: 'Home', descripcion: 'Panel general de ventas diarias' },
        { nombre: 'Facturas', icon: 'FileText', descripcion: 'Emisión de facturas y cotizaciones' },
        { nombre: 'Clientes', icon: 'Users', descripcion: 'Consulta y creación de clientes' },
        { nombre: 'Productos', icon: 'Package', descripcion: 'Consulta de precios y catálogo' }
      ];
    } else if (nombreRol.includes('caj') || nombreRol.includes('caja')) {
      return [
        { nombre: 'Resumen', icon: 'Home', descripcion: 'Panel general operativo' },
        { nombre: 'Facturas', icon: 'FileText', descripcion: 'Cobro y emisión de facturas' },
        { nombre: 'Caja', icon: 'CircleDollarSign', descripcion: 'Arqueo, apertura y cierre de caja' },
        { nombre: 'Clientes', icon: 'Users', descripcion: 'Consulta y registro de clientes' },
        { nombre: 'Productos', icon: 'Package', descripcion: 'Consulta de catálogo y disponibilidad' }
      ];
    } else if (nombreRol.includes('almacen') || nombreRol.includes('inventario') || nombreRol.includes('bodega')) {
      return [
        { nombre: 'Resumen', icon: 'Home', descripcion: 'Panel de control de almacén' },
        { nombre: 'Inventario', icon: 'Boxes', descripcion: 'Movimientos de kardex y existencias' },
        { nombre: 'Productos', icon: 'Package', descripcion: 'Gestión de catálogo de productos' },
        { nombre: 'Compras', icon: 'Truck', descripcion: 'Recepción y registro de compras' },
        { nombre: 'Proveedores', icon: 'Store', descripcion: 'Directorio de proveedores' }
      ];
    }

    return [
      { nombre: 'Resumen', icon: 'Home', descripcion: 'Panel informativo' },
      { nombre: 'Facturas', icon: 'FileText', descripcion: 'Facturación estándar' },
      { nombre: 'Clientes', icon: 'Users', descripcion: 'Directorio de clientes' },
      { nombre: 'Productos', icon: 'Package', descripcion: 'Consulta de productos' }
    ];
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

  abrirCrearFactura(tipo: 'Factura' | 'Cotizacion' = 'Cotizacion') {
    this.active = 'Facturas';
    this.modoEdicionCotizacion = false;
    this.cotizacionEditando = null;
    this.resetearNuevaFactura(tipo);
  }

  abrirCrearCotizacion() {
    this.abrirCrearFactura('Cotizacion');
  }

  volverAListadoFacturas() {
    this.modoEdicionCotizacion = false;
    this.cotizacionEditando = null;
    this.subTabFacturas = 'listado';
    this.loadVentas();
  }

  // Configuración SAR
  sarConfig: SARConfig | null = this.cargarSARConfigInicial();
  showSARConfigModal = false;
  showFacturaModal = false;
  showNuevoProductoModal = false;
  showNuevoClienteModal = false;
  showMovimientoInventarioModal = false;
  facturaSeleccionada: any = null;

  // Modales de Cotizaciones y Auditoría
  showEditarCotizacionModal = false;
  modoEdicionCotizacion = false;
  guardandoEdicionCotizacion = false;
  facturandoCotizacionId: number | null = null;
  cotizacionEditando: any = null;
  editarCotizacionForm: {
    clienteId: number | null;
    motivo: string;
    items: FacturaItemRow[];
  } = {
    clienteId: null as number | null,
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
    perfilId: 1,
    pantallas: [] as string[]
  };
  showEditarPermisosUsuarioModal = false;
  usuarioPermisosEditando: UsuarioItem | null = null;
  pantallasUsuarioEditando: string[] = [];

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
  filtroTipoAuditoria: string = 'TODOS';
  busquedaAuditoria: string = '';
  showHistorialEntidadModal: boolean = false;
  historialEntidadTitulo: string = '';
  historialEntidadSubtitulo: string = '';
  historialEntidadTipo: string = '';
  historialEntidadList: any[] = [];
  cargandoHistorialEntidad: boolean = false;
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
    clienteId: null as number | null,
    productoId: 1,
    cantidad: 1,
    metodoPagoId: 1,
    concretarVenta: true,
    emitirFactura: true
  };

  sarForm = {
    sucursalId: 1,
    rtn: '1804200401929',
    cai: '434A40-6203AB-EDBCE0-63BE03-09090C-EC',
    rangoDesde: '000-001-01-00000041',
    rangoHasta: '000-001-01-00000050',
    fechaLimiteEmision: '2026-11-10',
    resetearCorrelativo: false
  };

  // Facturación en vivo
  facturaForm = {
    tipoDocumento: 'Cotizacion' as 'Factura' | 'Cotizacion',
    clienteId: null as number | null,
    fecha: AppComponent.getFechaHoyHonduras(),
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
        tipoISV: 'ISV15',
        busqueda: 'Camara Domo IP 4MP IR 30m PoE',
        mostrarDropdown: false
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

  cantidadVentasMes = 0;
  stockBajoCount = 0;

  stats = [
    { label: 'Ventas del mes', value: 'L. 0.00', trend: 'Calculado en vivo', icon: 'CircleDollarSign', tone: 'green', modulo: 'Facturas' },
    { label: 'Productos activos', value: '0', trend: 'Catálogo sincronizado', icon: 'Package', tone: 'blue', modulo: 'Productos' },
    { label: 'Stock bajo', value: '0', trend: 'Requieren atención', icon: 'Boxes', tone: 'orange', modulo: 'Inventario' },
    { label: 'Estado de Caja', value: 'Cerrada', trend: 'Turno actual', icon: 'CircleDollarSign', tone: 'purple', modulo: 'Caja' }
  ];

  navegarDesdeStat(stat: any) {
    if (!stat) return;
    const destino = stat.modulo || (
      stat.label === 'Ventas del mes' || stat.label === 'Cantidad de ventas' ? 'Facturas' :
      stat.label === 'Productos activos' ? 'Productos' :
      stat.label === 'Stock bajo' ? 'Inventario' :
      stat.label === 'Estado de Caja' ? 'Caja' : ''
    );
    if (destino && this.puedeVerModulo(destino)) {
      if (stat.label === 'Stock bajo') {
        this.subTabInventario = 'stock';
      }
      this.setActive(destino);
    }
  }

  get visibleStats() {
    return this.stats
      .map((s, index) => {
        if (index === 2) {
          if (this.isFacturador) {
            return {
              label: 'Cantidad de ventas',
              value: this.cantidadVentasMes.toString(),
              trend: 'Facturas del mes',
              icon: 'FileText',
              tone: 'purple',
              modulo: 'Facturas'
            };
          } else {
            return {
              label: 'Stock bajo',
              value: (this.stockBajoCount || s.value || 0).toString(),
              trend: 'Requieren atención',
              icon: 'Boxes',
              tone: 'orange',
              modulo: 'Inventario'
            };
          }
        }
        return s;
      })
      .filter(s => s.label !== 'Estado de Caja' || this.puedeVerModulo('Caja'));
  }

  // =========================================================================
  // GESTIÓN DE IMPORTACIÓN Y EXPORTACIÓN DE TABLAS
  // =========================================================================
  showImportModal = false;
  importModalTipo: 'Productos' | 'Clientes' | 'Proveedores' = 'Productos';
  importandoDatos = false;
  registrosImportarPreview: any[] = [];
  archivoSeleccionadoNombre = '';
  columnasPreview: string[] = [];

  constructor() {
    if (!this.loginMode) {
      this.loadData();
    }
  }

  ngOnInit() {
    if (this.loginMode) {
      this.http.get(`${this.api}/Acceso/TestConexion`).subscribe({
        next: () => {},
        error: () => {}
      });
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

  obtenerMensajeError(err: any, fallback: string = 'Ha ocurrido un error inesperado.'): string {
    if (!err) return fallback;
    if (typeof err === 'string') return err;
    if (typeof err.error === 'string') return err.error;
    if (err.error?.mensaje) return err.error.mensaje;
    if (err.error?.message) return err.error.message;
    if (err.error?.error) return err.error.error;
    if (err.error?.errors && typeof err.error.errors === 'object') {
      const keys = Object.keys(err.error.errors);
      if (keys.length > 0) {
        const first = err.error.errors[keys[0]];
        return Array.isArray(first) ? first.join(', ') : String(first);
      }
    }
    return err.message || fallback;
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
    // Carga prioritaria y optimizada de métricas del panel Resumen (evita peticiones redundantes de Compras y Proveedores)
    this.loadProducts();
    this.loadInventory();
    this.loadVentas();
    if (this.puedeVerModulo('Caja')) {
      this.loadCajaEstado();
    }
    this.loadSARConfig();
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
        this.stockBajoCount = lowStock;
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
        this.calcularStatsVentas();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          this.ventas = [];
          this.stats[0].value = 'L. 0.00';
        }
      }
    });
  }

  esVentaAnulada(v: any): boolean {
    if (!v) return false;
    const est = String(v.estadoVenta || v.estado || (v.venta && v.venta.estadoVenta) || '').toLowerCase();
    return est.includes('anula') || est.includes('cancel') || v.estadoVentaId === 3 || (v.venta && v.venta.estadoVentaId === 3);
  }

  calcularStatsVentas() {
    const hoyHonduras = new Date(new Date().toLocaleString('en-US', { timeZone: 'America/Tegucigalpa' }));
    const mesActual = hoyHonduras.getMonth();
    const anoActual = hoyHonduras.getFullYear();

    const facturasMes = (this.ventas || []).filter(v => {
      // Excluir cotizaciones formalmente: solo facturas de venta
      const esCotizacion = v.esCotizacion || v.estadoVenta === 'Cotizacion' || (!v.numeroFactura && !!v.numeroCotizacion);
      // Excluir ventas anuladas
      const esAnulada = this.esVentaAnulada(v);
      if (esCotizacion || esAnulada) return false;

      // Evaluar la fecha de la factura en el huso de Honduras
      const rawFecha = v.fechaFactura || v.fechaVenta;
      if (!rawFecha) return false;
      const fechaDoc = this.parseFechaUtc(rawFecha);
      if (!fechaDoc) return false;

      const fechaLocal = new Date(fechaDoc.toLocaleString('en-US', { timeZone: 'America/Tegucigalpa' }));
      return fechaLocal.getMonth() === mesActual && fechaLocal.getFullYear() === anoActual;
    });

    this.cantidadVentasMes = facturasMes.length;
    const totalMes = facturasMes.reduce((acc, v) => acc + (Number(v.total) || 0), 0);
    this.stats[0].value = `L. ${totalMes.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  loadClientes() {
    this.http.get<any[]>(`${this.api}/Clientes`, this.getHeaders()).subscribe({
      next: (data) => {
        this.clientes = Array.isArray(data) ? data : [];
        if (this.clientes.length > 0) {
          if (!this.nuevaVenta.clienteId) this.nuevaVenta.clienteId = this.clientes[0].clienteId;
          // Por defecto la factura es Consumidor Final
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

  private cargarSARConfigInicial(): SARConfig {
    try {
      const cached = localStorage.getItem('webwave_sar_config');
      if (cached) {
        const parsed = JSON.parse(cached);
        if (parsed && parsed.cai) return parsed;
      }
    } catch {}

    return {
      id: 1,
      sucursalId: 1,
      rtn: '1804200401929',
      cai: '434A40-6203AB-EDBCE0-63BE03-09090C-EC',
      rangoDesde: '000-001-01-00000041',
      rangoHasta: '000-001-01-00000050',
      fechaLimiteEmision: '2026-11-10T00:00:00',
      correlativoActual: 40,
      siguienteFactura: '000-001-01-00000041',
      facturasDisponibles: 10,
      vigente: true
    };
  }

  loadSARConfig() {
    this.http.get<SARConfig>(`${this.api}/SAR/configuracion/1`, this.getHeaders()).subscribe({
      next: (data) => {
        if (data && data.cai) {
          this.sarConfig = data;
          try {
            localStorage.setItem('webwave_sar_config', JSON.stringify(data));
          } catch {}
          this.sarForm.rtn = data.rtn || this.sarForm.rtn;
          this.sarForm.cai = data.cai || this.sarForm.cai;
          this.sarForm.rangoDesde = data.rangoDesde || this.sarForm.rangoDesde;
          this.sarForm.rangoHasta = data.rangoHasta || this.sarForm.rangoHasta;
          if (data.fechaLimiteEmision) {
            this.sarForm.fechaLimiteEmision = data.fechaLimiteEmision.split('T')[0];
          }
        }
      },
      error: (err) => {
        console.warn('No se pudo actualizar configuración SAR del servidor:', err);
        if (!this.sarConfig) {
          this.sarConfig = this.cargarSARConfigInicial();
        }
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
  get clienteSeleccionado(): any {
    const c = this.clientes.find(cl => cl.clienteId == this.facturaForm.clienteId);
    if (c) return c;
    return { nombre: 'Consumidor Final', rtn: '0000-0000-00000', email: '—' };
  }

  get metodoPagoNombre(): string {
    switch (Number(this.facturaForm.metodoPagoId)) {
      case 1: return 'Efectivo';
      case 2: return 'Tarjeta de Crédito / Débito';
      case 3: return 'Transferencia Bancaria';
      default: return 'Efectivo';
    }
  }

  esProductoDuplicado(productoId: number | string): boolean {
    if (!productoId) return false;
    const pid = Number(productoId);
    const count = this.facturaForm.items.filter(item => Number(item.productoId) === pid).length;
    return count > 1;
  }

  get tieneProductosDuplicados(): boolean {
    const ids = this.facturaForm.items
      .map(i => Number(i.productoId))
      .filter(id => id > 0);
    return new Set(ids).size !== ids.length;
  }

  get siguienteCotizacionCalculada(): string {
    const nums = this.ventas
      .filter(v => v.esCotizacion && v.numeroCotizacion)
      .map(v => {
        const match = (v.numeroCotizacion || '').match(/\d+/);
        return match ? parseInt(match[0], 10) : 0;
      });
    const max = nums.length > 0 ? Math.max(...nums) : 0;
    return `COT-${(max + 1).toString().padStart(6, '0')}`;
  }

  get ultimaFacturaDisplay(): string {
    if (this.facturaForm.tipoDocumento === 'Cotizacion') {
      if (this.facturaEmitidaData?.numeroCotizacion) {
        return this.facturaEmitidaData.numeroCotizacion;
      }
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
    if (this.modoEdicionCotizacion && this.cotizacionEditando) {
      return this.cotizacionEditando.numeroCotizacion || this.cotizacionEditando.numeroFactura || ('#COT-' + this.cotizacionEditando.ventaId);
    }
    if (this.facturaForm.tipoDocumento === 'Cotizacion') {
      if (this.facturaEmitida && this.facturaEmitidaData?.numeroCotizacion) {
        return this.facturaEmitidaData.numeroCotizacion;
      }
      return this.siguienteCotizacionCalculada;
    }
    return this.sarConfig?.siguienteFactura || '000-001-01-00000001';
  }

  obtenerProductosFiltrados(busqueda: string | undefined): Product[] {
    const list = this.products || [];
    if (!busqueda || !busqueda.trim()) {
      return list.slice(0, 30);
    }
    const cleanSearch = this.normalizarTextoSinTildes(busqueda);
    const tokens = cleanSearch.split(/\s+/).filter(t => t.length > 0);

    return list.filter(p => {
      const nom = this.normalizarTextoSinTildes(p.nombre || '');
      const cod = this.normalizarTextoSinTildes(p.codigoBarras || p.codigo || '');
      const des = this.normalizarTextoSinTildes(p.descripcion || '');
      const allText = `${nom} ${cod} ${des}`;
      return tokens.every(token => allText.includes(token));
    }).slice(0, 30);
  }

  seleccionarProductoAutocompletar(item: FacturaItemRow, prod: Product) {
    item.productoId = prod.productoId!;
    item.nombre = prod.nombre || '';
    item.precio = prod.precioVenta || prod.precio || 0;
    item.tipoISV = prod.tipoISV || 'ISV15';
    item.busqueda = prod.nombre || '';
    item.mostrarDropdown = false;
  }

  seleccionarPrimerProducto(item: FacturaItemRow) {
    const filtrados = this.obtenerProductosFiltrados(item.busqueda);
    if (filtrados && filtrados.length > 0) {
      this.seleccionarProductoAutocompletar(item, filtrados[0]);
    }
  }

  limpiarBusquedaProducto(item: FacturaItemRow) {
    item.busqueda = '';
    item.mostrarDropdown = true;
  }

  onInputBusquedaProducto(item: FacturaItemRow) {
    item.mostrarDropdown = true;
  }

  cerrarDropdownConRetraso(item: FacturaItemRow) {
    setTimeout(() => {
      item.mostrarDropdown = false;
      const p = (this.products || []).find(x => x.productoId === item.productoId);
      if (p) {
        item.busqueda = p.nombre || '';
      }
    }, 250);
  }

  abrirDropdownProducto(item: FacturaItemRow, event?: any) {
    this.facturaForm.items.forEach(it => {
      if (it !== item) it.mostrarDropdown = false;
    });
    item.mostrarDropdown = true;
    if (event?.target && typeof event.target.select === 'function') {
      try {
        event.target.select();
      } catch (e) {}
    }
  }

  onProductoCambio(item: FacturaItemRow) {
    const p = this.products.find(x => x.productoId == item.productoId);
    if (p) {
      item.nombre = p.nombre || '';
      item.precio = p.precioVenta || p.precio || 0;
      item.tipoISV = p.tipoISV || 'ISV15';
      item.busqueda = p.nombre || '';
    }
  }

  agregarItemFactura() {
    this.facturaForm.items.forEach(it => it.mostrarDropdown = false);
    this.facturaForm.items.unshift({
      productoId: 0,
      nombre: '',
      cantidad: 1,
      precio: 0,
      descuento: 0,
      tipoISV: 'ISV15',
      busqueda: '',
      mostrarDropdown: true
    });
  }


  async confirmarLimpiarTodasLasFacturas() {
    const confirmado = await this.confirmarAccion(
      '¿Eliminar Todas las Facturas?',
      'Esta acción eliminará de forma permanente todas las facturas y ventas de prueba de la base de datos y restablecerá el correlativo del SAR al inicio. ¿Deseas continuar?',
      'Sí, eliminar todas las facturas',
      'Cancelar',
      true
    );
    if (!confirmado) return;

    this.http.delete<any>(`${this.api}/Ventas/Facturas/LimpiarTodas`, this.getHeaders()).subscribe({
      next: () => {
        this.notificarExito('Facturas Eliminadas', 'Se eliminaron exitosamente todas las facturas de la base de datos y se restableció el correlativo.');
        this.loadVentas();
        this.loadSARConfig();
        this.loadProducts();
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          const detalle = this.obtenerMensajeError(err, 'No se pudieron eliminar las facturas.');
          this.notificarError('Error al Limpiar Facturas', detalle);
        }
      }
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
    if (this.generandoFactura) return;
    if (this.facturaForm.items.length === 0) {
      this.notificarAdvertencia('Documento vacío', 'Agrega al menos un producto al documento.');
      return;
    }

    const itemSinProducto = this.facturaForm.items.find(i => !i.productoId || Number(i.productoId) <= 0);
    if (itemSinProducto) {
      this.notificarAdvertencia('Producto no seleccionado', 'Por favor selecciona un producto válido para todos los renglones antes de procesar el documento.');
      return;
    }

    if (this.tieneProductosDuplicados) {
      this.notificarError('Producto duplicado', 'No puedes repetir el mismo producto en varios renglones. Los ítems repetidos están resaltados con bordes rojos. Ajusta la cantidad del ítem existente o selecciona un producto diferente.');
      return;
    }

    this.generandoFactura = true;
    const isCotizacion = this.facturaForm.tipoDocumento === 'Cotizacion';
    const payload = {
      clienteId: Number(this.facturaForm.clienteId) || (this.clientes.find(c => (c.nombre && c.nombre.toLowerCase().includes('consumidor final')) || (c.rtn && c.rtn.includes('0000')))?.clienteId || 0),
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
          // Actualizar inmediatamente el listado local de ventas para refrescar el correlativo al instante
          this.ventas = [ventaCreada, ...this.ventas.filter(v => v.ventaId !== ventaCreada.ventaId)];
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

  nuevaFacturaFormulario(tipo: 'Factura' | 'Cotizacion' = 'Cotizacion') {
    this.facturaEmitida = false;
    this.facturaEmitidaData = null;
    this.loadSARConfig();
    this.loadVentas();
    const firstProd = this.products.find(p => p.nombre?.toLowerCase().includes('camara') || p.nombre?.toLowerCase().includes('magnalock') || p.nombre?.toLowerCase().includes('switch')) || this.products[0];
    this.facturaForm = {
      tipoDocumento: tipo,
      clienteId: null,
      fecha: AppComponent.getFechaHoyHonduras(),
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
          tipoISV: firstProd?.tipoISV || 'ISV15',
          busqueda: firstProd?.nombre || 'Equipo Tecnológico',
          mostrarDropdown: false
        }
      ]
    };
  }

  // =========================================================================
  // IMPRESIÓN Y EXPORTACIÓN A PDF DE FACTURAS Y COTIZACIONES
  // =========================================================================
  registrarLogDocumento(tipo: 'FACTURA' | 'COTIZACION', entidadId: number, referencia: string, accion: string, detalle: string, total: number) {
    if (!entidadId) return;
    const payload = {
      tipo,
      entidadId,
      referencia,
      accion,
      detalle,
      motivo: accion.includes('IMPRESI') ? 'Impresión de comprobante' : 'Descarga digital PDF',
      total,
      usuario: this.currentUser?.name || 'Usuario'
    };
    this.http.post(`${this.api}/Auditoria/registrar-evento`, payload, this.getHeaders()).subscribe({
      next: () => {},
      error: (e) => console.warn('No se pudo registrar auditoría de documento:', e)
    });
  }

  imprimirFacturaEnVivo() {
    if (this.facturaEmitida && this.facturaEmitidaData) {
      const isCot = this.facturaForm.tipoDocumento === 'Cotizacion';
      const ref = isCot ? (this.facturaEmitidaData.numeroCotizacion || `COT-${this.facturaEmitidaData.ventaId}`) : (this.facturaEmitidaData.numeroFactura || `FAC-${this.facturaEmitidaData.ventaId}`);
      this.registrarLogDocumento(
        isCot ? 'COTIZACION' : 'FACTURA',
        this.facturaEmitidaData.ventaId,
        ref,
        isCot ? 'IMPRESIÓN COTIZACIÓN' : 'IMPRESIÓN FACTURA',
        `Impresión de comprobante oficial realizada por ${this.currentUser?.name || 'Usuario'}.`,
        this.facturaEmitidaData.total || this.totalFactura
      );
    }
    this.ejecutarImpresionElemento('invoice-preview-area');
  }

  descargarFacturaEnVivoPDF() {
    if (this.facturaEmitida && this.facturaEmitidaData) {
      const isCot = this.facturaForm.tipoDocumento === 'Cotizacion';
      const ref = isCot ? (this.facturaEmitidaData.numeroCotizacion || `COT-${this.facturaEmitidaData.ventaId}`) : (this.facturaEmitidaData.numeroFactura || `FAC-${this.facturaEmitidaData.ventaId}`);
      this.registrarLogDocumento(
        isCot ? 'COTIZACION' : 'FACTURA',
        this.facturaEmitidaData.ventaId,
        ref,
        isCot ? 'DESCARGA COTIZACIÓN PDF' : 'DESCARGA FACTURA PDF',
        `Descarga de documento en PDF realizada por ${this.currentUser?.name || 'Usuario'}.`,
        this.facturaEmitidaData.total || this.totalFactura
      );
    }
    const num = this.facturaEmitida
      ? ((this.facturaForm.tipoDocumento === 'Cotizacion' ? this.facturaEmitidaData?.numeroCotizacion : this.facturaEmitidaData?.numeroFactura) || 'documento')
      : (this.nuevaFacturaDisplay || 'documento');
    const filename = `${this.facturaForm.tipoDocumento === 'Cotizacion' ? 'Cotizacion' : 'Factura'}_${num}.pdf`;
    this.generarPDFDesdeElemento('invoice-preview-area', filename);
  }

  imprimirFacturaModal() {
    if (this.facturaSeleccionada) {
      const id = this.facturaSeleccionada.ventaId || this.facturaSeleccionada.id;
      const ref = this.facturaSeleccionada.numeroFactura || `FAC-${id}`;
      this.registrarLogDocumento(
        'FACTURA',
        id,
        ref,
        'IMPRESIÓN FACTURA',
        `Impresión de Factura Fiscal SAR Nº ${ref} realizada por ${this.currentUser?.name || 'Usuario'}.`,
        this.facturaSeleccionada.total || 0
      );
    }
    this.ejecutarImpresionElemento('invoice-modal-print-area');
  }

  descargarFacturaModalPDF() {
    if (this.facturaSeleccionada) {
      const id = this.facturaSeleccionada.ventaId || this.facturaSeleccionada.id;
      const ref = this.facturaSeleccionada.numeroFactura || `FAC-${id}`;
      this.registrarLogDocumento(
        'FACTURA',
        id,
        ref,
        'DESCARGA FACTURA PDF',
        `Descarga de Factura Fiscal SAR Nº ${ref} en PDF por ${this.currentUser?.name || 'Usuario'}.`,
        this.facturaSeleccionada.total || 0
      );
    }
    const num = this.facturaSeleccionada?.numeroFactura || ('FAC-' + (this.facturaSeleccionada?.ventaId || '01'));
    this.generarPDFDesdeElemento('invoice-modal-print-area', `Factura_${num}.pdf`);
  }

  imprimirCotizacionModal() {
    if (this.cotizacionSeleccionada) {
      const id = this.cotizacionSeleccionada.ventaId;
      const ref = this.cotizacionSeleccionada.numeroCotizacion || `COT-${id}`;
      this.registrarLogDocumento(
        'COTIZACION',
        id,
        ref,
        'IMPRESIÓN COTIZACIÓN',
        `Impresión de Cotización Nº ${ref} realizada por ${this.currentUser?.name || 'Usuario'}.`,
        this.cotizacionSeleccionada.total || 0
      );
    }
    this.ejecutarImpresionElemento('cotizacion-modal-print-area');
  }

  descargarCotizacionModalPDF() {
    if (this.cotizacionSeleccionada) {
      const id = this.cotizacionSeleccionada.ventaId;
      const ref = this.cotizacionSeleccionada.numeroCotizacion || `COT-${id}`;
      this.registrarLogDocumento(
        'COTIZACION',
        id,
        ref,
        'DESCARGA COTIZACIÓN PDF',
        `Descarga de Cotización Nº ${ref} en PDF por ${this.currentUser?.name || 'Usuario'}.`,
        this.cotizacionSeleccionada.total || 0
      );
    }
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

    // Sanitizar el nodo clonado eliminando scripts, iframes y handlers de eventos inline
    const clonedEl = el.cloneNode(true) as HTMLElement;
    clonedEl.querySelectorAll('script, iframe, object, embed, link[rel="import"]').forEach(node => node.remove());
    clonedEl.querySelectorAll('*').forEach(node => {
      Array.from(node.attributes).forEach(attr => {
        if (attr.name.toLowerCase().startsWith('on') || attr.value.toLowerCase().includes('javascript:')) {
          node.removeAttribute(attr.name);
        }
      });
    });

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
            .preview-note-box { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 9px 12px; margin-bottom: 14px; font-size: 10.5px; line-height: 1.45; color: #334155; }
            .preview-table { width: 100%; border-collapse: collapse; margin-bottom: 14px; font-size: 11.5px; }
            .preview-table th { background: #f8fafc; border-bottom: 1.5px solid #cbd5e1; padding: 8px 6px; text-align: left; font-size: 11px !important; font-weight: 700 !important; color: #475569 !important; }
            .preview-table td { padding: 8px 6px; border-bottom: 1px solid #f1f5f9; font-size: 11.5px; }
            .preview-summary { margin-left: auto; width: 250px; display: grid; gap: 5px; border-top: 1px solid #cbd5e1; padding-top: 10px; }
            .preview-summary-row { display: flex; justify-content: space-between; font-size: 11.5px; color: #475569; }
            .preview-summary-total { display: flex; justify-content: space-between; font-size: 14px; font-weight: 800; border-top: 2px solid #0f172a; padding-top: 6px; margin-top: 4px; color: #0f172a; }
            .preview-footer-words { background: #f8fafc; border: 1px dashed #cbd5e1; padding: 10px 14px; border-radius: 8px; margin-top: 14px; font-size: 11px; }
            .badge-cotizacion { background: #f3e8ff; color: #7e22ce; border: 1px solid #e9d5ff; padding: 3px 7px; border-radius: 4px; font-size: 10px; font-weight: 700; display: inline-block; }
            .factura-paper-sheet { position: relative !important; overflow: hidden !important; }
            .watermark-anulada { position: absolute !important; top: 48% !important; left: 50% !important; transform: translate(-50%, -50%) rotate(-28deg) !important; pointer-events: none !important; user-select: none !important; display: flex !important; align-items: center !important; justify-content: center !important; border: 8px solid rgba(220, 38, 38, 0.35) !important; border-radius: 18px !important; padding: 16px 52px !important; background: rgba(254, 226, 226, 0.15) !important; z-index: 99 !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
            .watermark-anulada span { font-family: 'Manrope', sans-serif !important; font-size: 78px !important; font-weight: 900 !important; color: rgba(220, 38, 38, 0.32) !important; letter-spacing: 0.18em !important; text-transform: uppercase !important; line-height: 1 !important; }
            @page { margin: 12mm; size: auto; }
          </style>
        </head>
        <body>
          <div>${clonedEl.innerHTML}</div>
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

  resetearNuevaFactura(tipo: 'Factura' | 'Cotizacion' = 'Cotizacion') {
    this.modoEdicionCotizacion = false;
    this.cotizacionEditando = null;
    this.facturaEmitida = false;
    this.facturaEmitidaData = null;
    this.nuevaFacturaFormulario(tipo);
    this.subTabFacturas = 'crear';
  }

  generarFacturaFiscal() {
    this.generarFacturaEnVivo();
  }

  // =========================================================================
  // GESTIÓN Y EDICIÓN DE COTIZACIONES + HISTORIAL DE AUDITORÍA
  // =========================================================================
  abrirModalEditarCotizacion(venta: any) {
    if (!venta) return;
    if (this.esVentaAnulada(venta)) {
      this.notificarAdvertencia('Documento Anulado', 'No se puede editar un documento que ha sido anulado.');
      return;
    }

    if (!venta.esCotizacion) {
      this.notificarAdvertencia(
        'Factura Fiscal Inmutable',
        'Por disposiciones fiscales del SAR, una factura emitida con CAI no puede ser modificada. Si requiere corregirla, debe anularla y emitir una nueva factura.'
      );
      return;
    }

    this.active = 'Facturas';
    this.subTabFacturas = 'crear';
    this.facturaEmitida = false;
    this.facturaEmitidaData = null;
    this.modoEdicionCotizacion = true;
    this.cotizacionEditando = venta;

    if (!this.products || this.products.length === 0) {
      this.loadProducts();
    }
    if (!this.clientes || this.clientes.length === 0) {
      this.loadClientes();
    }

    let fechaDoc = AppComponent.getFechaHoyHonduras();
    if (venta.fechaVenta) {
      try {
        const d = new Date(venta.fechaVenta);
        if (!isNaN(d.getTime())) {
          fechaDoc = d.toISOString().substring(0, 10);
        }
      } catch (e) {}
    }

    const itemsEditados: FacturaItemRow[] = (venta.detalles || []).map((d: any) => {
      const p = this.products.find(x => x.productoId == d.productoId);
      const nombreP = d.producto || p?.nombre || `Producto #${d.productoId}`;
      return {
        productoId: Number(d.productoId),
        nombre: nombreP,
        cantidad: Number(d.cantidad) || 1,
        precio: d.precio !== undefined ? Number(d.precio) : Number(p?.precioVenta || 0),
        descuento: Number(d.descuento || 0),
        tipoISV: p?.tipoISV || 'ISV15',
        busqueda: nombreP,
        mostrarDropdown: false
      };
    });

    if (itemsEditados.length === 0 && this.products.length > 0) {
      const p = this.products[0];
      itemsEditados.push({
        productoId: p.productoId || 1,
        nombre: p.nombre || '',
        cantidad: 1,
        precio: p.precioVenta || 0,
        descuento: 0,
        tipoISV: p.tipoISV || 'ISV15'
      });
    }

    const metodoPago = (venta.pagos && venta.pagos.length > 0) ? venta.pagos[0].metodoPagoId : (venta.metodoPagoId || 1);

    this.facturaForm = {
      tipoDocumento: venta.esCotizacion ? 'Cotizacion' : 'Factura',
      clienteId: venta.clienteId ? Number(venta.clienteId) : null,
      fecha: fechaDoc,
      metodoPagoId: Number(metodoPago) || 1,
      moneda: 'Lempiras (HNL)',
      notas: venta.observaciones || '',
      notasPie: venta.notasPie || 'Gracias por su preferencia. Garantía de 1 año en equipos de seguridad electrónica y redes.',
      mostrarNotas: !!venta.observaciones,
      mostrarNotasPie: false,
      items: itemsEditados.length > 0 ? itemsEditados : this.facturaForm.items
    };

    try {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (e) {}

    if (venta.esCotizacion) {
      this.notificarInfo(
        'Modo Edición Activado',
        `Se cargó la cotización ${venta.numeroCotizacion || ('#COT-' + venta.ventaId)} completa en el diseñador interactivo. Modifique los datos y presione "Guardar Cambios".`
      );
    } else {
      this.notificarInfo(
        'Factura Cargada',
        `Se cargaron todos los datos de la Factura ${venta.numeroFactura || venta.ventaId} en el editor interactivo.`
      );
    }
  }

  cambiarTipoDocumentoWorkspace(tipo: 'Factura' | 'Cotizacion') {
    if (this.facturaEmitida && this.facturaEmitidaData?.esCotizacion && tipo === 'Factura') {
      Swal.fire({
        title: '¿Convertir a Factura Fiscal SAR?',
        text: `Ya se ha generado la cotización ${this.facturaEmitidaData.numeroCotizacion || ''}. ¿Deseas convertir esta misma cotización en Factura Fiscal oficial (con CAI y descuento de stock), o iniciar una nueva factura en blanco?`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#1f8a67',
        cancelButtonColor: '#0284c7',
        confirmButtonText: 'Sí, Facturar Cotización',
        cancelButtonText: 'Nueva Factura en Blanco'
      }).then((result) => {
        if (result.isConfirmed) {
          this.facturarCotizacionDesdeVentas(this.facturaEmitidaData);
        } else if (result.dismiss === Swal.DismissReason.cancel) {
          this.resetearNuevaFactura('Factura');
        }
      });
      return;
    }
    this.facturaForm.tipoDocumento = tipo;
  }

  guardarEdicionCotizacionDesdeWorkspace() {
    if (!this.cotizacionEditando) return;
    if (!this.facturaForm.items || this.facturaForm.items.length === 0) {
      this.notificarAdvertencia('Documento vacío', 'Debe incluir al menos un producto.');
      return;
    }

    if (this.facturaForm.items.some(i => (i.cantidad || 0) <= 0)) {
      this.notificarAdvertencia('Cantidad Inválida', 'La cantidad de cada ítem debe ser mayor a 0.');
      return;
    }

    this.guardandoEdicionCotizacion = true;
    const payload = {
      clienteId: Number(this.facturaForm.clienteId || this.cotizacionEditando.clienteId || 1),
      motivo: 'Ajuste de productos y precios desde el editor interactivo de facturación',
      detalles: this.facturaForm.items.map(i => ({
        productoId: Number(i.productoId),
        cantidad: Number(i.cantidad),
        precioUnitario: Number(i.precio),
        descuento: Number(i.descuento || 0)
      }))
    };

    this.http.put<any>(`${this.api}/Ventas/${this.cotizacionEditando.ventaId}/Cotizacion`, payload, this.getHeaders()).subscribe({
      next: (ventaActualizada) => {
        this.guardandoEdicionCotizacion = false;
        this.modoEdicionCotizacion = false;
        this.cotizacionEditando = null;
        this.notificarExito(
          'Cotización Actualizada',
          `¡Documento ${ventaActualizada.numeroCotizacion || ''} actualizado exitosamente!`
        );
        this.volverAListadoFacturas();
      },
      error: (err) => {
        this.guardandoEdicionCotizacion = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error al Guardar', 'Error al modificar cotización: ' + (err.error?.mensaje || err.message));
        }
      }
    });
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
    if (this.esVentaAnulada(venta)) {
      this.notificarAdvertencia('Cotización Anulada', 'No se puede facturar una cotización que ha sido anulada.');
      return;
    }
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
        if (this.isSuperUsuario) { this.loadAuditoriaLogs(); }
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
        if (this.isSuperUsuario) { this.loadAuditoriaLogs(); }
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
    const perfilInicial = this.perfiles[0]?.perfilId || 1;
    const defaults = this.getDefaultScreensForRole(this.perfiles[0]?.nombre);
    this.nuevoUsuarioForm = {
      nombre: '',
      email: '',
      password: '',
      perfilId: perfilInicial,
      pantallas: [...defaults]
    };
    this.showNuevoUsuarioModal = true;
  }

  onPerfilChangeNuevoUsuario() {
    const perfil = this.perfiles.find(p => p.perfilId === Number(this.nuevoUsuarioForm.perfilId));
    const defaults = this.getDefaultScreensForRole(perfil?.nombre);
    this.nuevoUsuarioForm.pantallas = [...defaults];
  }

  abrirModalPermisosUsuario(user: UsuarioItem) {
    this.usuarioPermisosEditando = user;
    const emailKey = (user.email || '').toLowerCase();
    const stored = localStorage.getItem('vt_user_screens_' + emailKey);
    if (stored) {
      try {
        const parsed = JSON.parse(stored);
        if (Array.isArray(parsed) && parsed.length > 0) {
          this.pantallasUsuarioEditando = [...parsed];
        } else {
          this.pantallasUsuarioEditando = this.getDefaultScreensForRole(user.perfil);
        }
      } catch (e) {
        this.pantallasUsuarioEditando = this.getDefaultScreensForRole(user.perfil);
      }
    } else {
      this.pantallasUsuarioEditando = this.getDefaultScreensForRole(user.perfil);
    }
    this.showEditarPermisosUsuarioModal = true;
  }

  guardarPermisosUsuario() {
    if (!this.usuarioPermisosEditando) return;
    const emailKey = (this.usuarioPermisosEditando.email || '').toLowerCase();
    localStorage.setItem('vt_user_screens_' + emailKey, JSON.stringify(this.pantallasUsuarioEditando));
    this.showEditarPermisosUsuarioModal = false;
    this.notificarExito('Permisos Actualizados', `Pantallas autorizadas guardadas para ${this.usuarioPermisosEditando.nombre}.`);
  }

  isPantallaSeleccionada(label: string, esEdicion: boolean = false): boolean {
    if (esEdicion) {
      return this.pantallasUsuarioEditando.includes(label);
    }
    return this.nuevoUsuarioForm.pantallas ? this.nuevoUsuarioForm.pantallas.includes(label) : false;
  }

  togglePantalla(label: string, esEdicion: boolean = false) {
    if (esEdicion) {
      if (this.pantallasUsuarioEditando.includes(label)) {
        this.pantallasUsuarioEditando = this.pantallasUsuarioEditando.filter(p => p !== label);
      } else {
        this.pantallasUsuarioEditando.push(label);
      }
    } else {
      if (!this.nuevoUsuarioForm.pantallas) this.nuevoUsuarioForm.pantallas = [];
      if (this.nuevoUsuarioForm.pantallas.includes(label)) {
        this.nuevoUsuarioForm.pantallas = this.nuevoUsuarioForm.pantallas.filter((p: string) => p !== label);
      } else {
        this.nuevoUsuarioForm.pantallas.push(label);
      }
    }
  }

  seleccionarTodasPantallas(esEdicion: boolean = false) {
    const todas = this.todasLasPantallasDisponibles.map(p => p.label);
    if (esEdicion) {
      this.pantallasUsuarioEditando = [...todas];
    } else {
      this.nuevoUsuarioForm.pantallas = [...todas];
    }
  }

  deseleccionarTodasPantallas(esEdicion: boolean = false) {
    if (esEdicion) {
      this.pantallasUsuarioEditando = [];
    } else {
      this.nuevoUsuarioForm.pantallas = [];
    }
  }

  getPermisosUsuarioCount(user: UsuarioItem): number {
    const emailKey = (user.email || '').toLowerCase();
    const stored = localStorage.getItem('vt_user_screens_' + emailKey);
    if (stored) {
      try {
        const screens = JSON.parse(stored);
        if (Array.isArray(screens) && screens.length > 0) return screens.length;
      } catch (e) {}
    }
    return this.getDefaultScreensForRole(user.perfil).length;
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
        if (this.nuevoUsuarioForm.pantallas && this.nuevoUsuarioForm.pantallas.length > 0) {
          localStorage.setItem('vt_user_screens_' + payload.email.toLowerCase(), JSON.stringify(this.nuevoUsuarioForm.pantallas));
        }
        this.notificarExito('Usuario Creado', '¡Usuario creado exitosamente con sus pantallas configuradas!');
        this.loadUsuarios();
      },
      error: (err) => {
        this.guardandoUsuario = false;
        if (!this.handleAuthError(err)) {
          const detalle = this.obtenerMensajeError(err, 'No se pudo registrar el usuario.');
          this.notificarError('No se pudo crear el usuario', detalle);
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
        if (this.isSuperUsuario) { this.loadAuditoriaLogs(); }
      },
      error: (err) => {
        if (!this.handleAuthError(err)) {
          const detalle = this.obtenerMensajeError(err, 'No se pudo eliminar el usuario.');
          this.notificarError('Error al Eliminar', detalle);
        }
      }
    });
  }


  // Control de visualización de contraseñas de usuarios (Solo Administrador)
  clavesVisibles: { [usuarioId: number]: boolean } = {};
  toggleVerClave(usuarioId: number) {
    this.clavesVisibles[usuarioId] = !this.clavesVisibles[usuarioId];
  }

  copiarClave(clave: string) {
    if (!clave) return;
    navigator.clipboard.writeText(clave);
    this.notificarExito('Copiado', 'Contraseña copiada al portapapeles.');
  }

  showEditarUsuarioModal = false;
  guardandoEdicionUsuario = false;
  usuarioEditandoForm = {
    usuarioId: 0,
    nombre: '',
    email: '',
    password: '',
    perfilId: 1
  };

  abrirModalEditarUsuario(u: UsuarioItem) {
    this.usuarioEditandoForm = {
      usuarioId: u.usuarioId,
      nombre: u.nombre,
      email: u.email,
      password: '',
      perfilId: u.perfilId || (u.perfil === 'Administrador' ? 1 : 2)
    };
    this.showEditarUsuarioModal = true;
  }

  guardarEdicionUsuario() {
    if (!this.usuarioEditandoForm.nombre.trim()) {
      this.notificarAdvertencia('Campo Requerido', 'El nombre es obligatorio.');
      return;
    }
    if (!this.usuarioEditandoForm.email.trim()) {
      this.notificarAdvertencia('Campo Requerido', 'El correo electrónico es obligatorio.');
      return;
    }
    if (this.usuarioEditandoForm.password && this.usuarioEditandoForm.password.length < 8) {
      this.notificarAdvertencia('Contraseña Corta', 'La contraseña debe tener al menos 8 caracteres.');
      return;
    }

    this.guardandoEdicionUsuario = true;
    const payload = {
      nombre: this.usuarioEditandoForm.nombre.trim(),
      email: this.usuarioEditandoForm.email.trim(),
      contraseña: this.usuarioEditandoForm.password ? this.usuarioEditandoForm.password.trim() : undefined,
      perfilId: Number(this.usuarioEditandoForm.perfilId)
    };

    this.http.put<any>(`${this.api}/Acceso/Usuarios/${this.usuarioEditandoForm.usuarioId}`, payload, this.getHeaders()).subscribe({
      next: () => {
        this.guardandoEdicionUsuario = false;
        this.showEditarUsuarioModal = false;
        this.notificarExito('Usuario Actualizado', 'Los datos y contraseña del usuario se actualizaron correctamente.');
        this.loadUsuarios();
      },
      error: (err) => {
        this.guardandoEdicionUsuario = false;
        if (!this.handleAuthError(err)) {
          const detalle = this.obtenerMensajeError(err, 'No se pudo actualizar el usuario.');
          this.notificarError('Error al Actualizar', detalle);
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
    if (!this.productoEditando.nombre || !this.productoEditando.nombre.trim()) {
      this.notificarAdvertencia('Campo requerido', 'El nombre del producto es obligatorio.');
      return;
    }

    const nombreNorm = this.productoEditando.nombre.trim().replace(/\s+/g, ' ').toLowerCase();
    const existeDuplicado = (this.products || []).some((p: any) =>
      p.productoId !== this.productoEditando.productoId &&
      (p.nombre || '').trim().replace(/\s+/g, ' ').toLowerCase() === nombreNorm
    );
    if (existeDuplicado) {
      this.notificarAdvertencia('Producto Duplicado', `Ya existe otro producto registrado con el nombre "${this.productoEditando.nombre.trim()}".`);
      return;
    }

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
        if (this.isSuperUsuario) {
          this.loadAuditoriaLogs();
        }
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
        if (this.isSuperUsuario) {
          this.loadAuditoriaLogs();
        }
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
    if (!this.clienteEditando.nombre || !this.clienteEditando.nombre.trim()) {
      this.notificarAdvertencia('Campo requerido', 'El nombre del cliente es obligatorio.');
      return;
    }

    const rtnIngresado = (this.clienteEditando.rtn || '').trim();
    const rtnLimpio = rtnIngresado.replace(/[-\s]/g, '');

    if (rtnLimpio && rtnLimpio !== '00000000000000' && rtnLimpio !== '000000000000') {
      const duplicado = (this.clientes || []).find(c => {
        if (c.clienteId === this.clienteEditando.clienteId) return false;
        const cRtn = (c.rtn || '').trim();
        const cLimpio = cRtn.replace(/[-\s]/g, '');
        return cLimpio && cLimpio !== '00000000000000' && (cRtn.toLowerCase() === rtnIngresado.toLowerCase() || cLimpio === rtnLimpio);
      });
      if (duplicado) {
        this.notificarAdvertencia('RTN ya registrado', `Ya existe otro cliente (${duplicado.nombre}) registrado con el RTN "${rtnIngresado}".`);
        return;
      }
    }

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
        if (this.isSuperUsuario) { this.loadAuditoriaLogs(); }
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

    const rtnIngresado = (this.nuevoCliente.rtn || '').trim();
    const rtnLimpio = rtnIngresado.replace(/[-\s]/g, '');

    if (rtnLimpio && rtnLimpio !== '00000000000000' && rtnLimpio !== '000000000000') {
      const duplicado = (this.clientes || []).find(c => {
        const cRtn = (c.rtn || '').trim();
        const cLimpio = cRtn.replace(/[-\s]/g, '');
        return cLimpio && cLimpio !== '00000000000000' && (cRtn.toLowerCase() === rtnIngresado.toLowerCase() || cLimpio === rtnLimpio);
      });
      if (duplicado) {
        this.notificarAdvertencia('RTN ya registrado', `Ya existe un cliente (${duplicado.nombre}) registrado con el RTN "${rtnIngresado}".`);
        return;
      }
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
        if (this.isSuperUsuario) { this.loadAuditoriaLogs(); }
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

  normalizarTextoSinTildes(texto: string | null | undefined): string {
    if (!texto) return '';
    return texto
      .trim()
      .replace(/\s+/g, ' ')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase();
  }

  guardarProducto() {
    if (!this.nuevoProducto.nombre || !this.nuevoProducto.nombre.trim()) {
      this.notificarAdvertencia('Campo requerido', 'El nombre del producto es obligatorio.');
      return;
    }

    const nombreNorm = this.nuevoProducto.nombre.trim().replace(/\s+/g, ' ').toLowerCase();
    const existeDuplicado = (this.products || []).some((p: any) =>
      (p.nombre || '').trim().replace(/\s+/g, ' ').toLowerCase() === nombreNorm
    );
    if (existeDuplicado) {
      this.notificarAdvertencia('Producto Duplicado', `Ya existe un producto registrado con el nombre "${this.nuevoProducto.nombre.trim()}".`);
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
        if (this.isSuperUsuario) {
          this.loadAuditoriaLogs();
        }
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
          this.http.get<any>(`${this.api}/Ventas/${venta.ventaId}`, this.getHeaders()).subscribe({
            next: (v) => {
              const d = v || venta;
              this.facturaSeleccionada = {
                numeroFactura: d.numeroFactura || `VENTA-#${d.ventaId}`,
                cai: d.cai || (this.sarConfig?.cai || '—'),
                fechaEmision: d.fechaFactura || d.fechaVenta,
                nombreCliente: d.cliente || 'Consumidor Final',
                rtnCliente: d.clienteRtn || '00000000000000',
                total: d.total || 0,
                montoExento: d.montoExento || 0,
                montoGravado15: d.montoGravado15 || ((d.total || 0) / 1.15),
                isv15: d.isv15 || ((d.total || 0) - ((d.total || 0) / 1.15)),
                rangoDesde: this.sarConfig?.rangoDesde || '000-001-01-00000001',
                rangoHasta: this.sarConfig?.rangoHasta || '000-001-01-00005000',
                fechaLimiteEmision: this.sarConfig?.fechaLimiteEmision || '2027-12-31',
                detalles: (d.detalles || []).map((det: any) => ({
                  productoId: det.productoId,
                  nombre: det.producto || det.nombre || `Producto #${det.productoId}`,
                  cantidad: det.cantidad,
                  precio: det.precio,
                  total: det.total || (det.cantidad * det.precio),
                  descuento: 0,
                  tipoISV: 'ISV15'
                })),
                venta: d,
                estadoVenta: d.estadoVenta
              };
              this.showFacturaModal = true;
            },
            error: () => {
              this.notificarInfo('Sin Factura', 'Esta venta aún no tiene una factura fiscal emitida.');
            }
          });
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

  
  get totalPaginasProductos(): number {
    const total = Math.ceil((this.filteredProducts || []).length / this.tamanoPaginaProductos);
    return total > 0 ? total : 1;
  }

  get paginatedProducts(): any[] {
    const items = this.filteredProducts || [];
    const total = this.totalPaginasProductos;
    if (this.paginaActualProductos > total) {
      this.paginaActualProductos = total;
    }
    if (this.paginaActualProductos < 1) {
      this.paginaActualProductos = 1;
    }
    const start = (this.paginaActualProductos - 1) * this.tamanoPaginaProductos;
    return items.slice(start, start + this.tamanoPaginaProductos);
  }

  get inicioProductosPaginados(): number {
    if (!this.filteredProducts || this.filteredProducts.length === 0) return 0;
    return (this.paginaActualProductos - 1) * this.tamanoPaginaProductos + 1;
  }

  get finProductosPaginados(): number {
    if (!this.filteredProducts || this.filteredProducts.length === 0) return 0;
    return Math.min(this.paginaActualProductos * this.tamanoPaginaProductos, this.filteredProducts.length);
  }

  get paginasDisponiblesProductos(): number[] {
    const total = this.totalPaginasProductos;
    const actual = this.paginaActualProductos;
    const paginas: number[] = [];
    const inicio = Math.max(1, actual - 2);
    const fin = Math.min(total, actual + 2);
    for (let i = inicio; i <= fin; i++) {
      paginas.push(i);
    }
    return paginas;
  }

  cambiarPaginaProductos(p: number): void {
    if (p >= 1 && p <= this.totalPaginasProductos) {
      this.paginaActualProductos = p;
    }
  }

  paginaAnteriorProductos(): void {
    if (this.paginaActualProductos > 1) {
      this.paginaActualProductos--;
    }
  }

  paginaSiguienteProductos(): void {
    if (this.paginaActualProductos < this.totalPaginasProductos) {
      this.paginaActualProductos++;
    }
  }

  formatearTipoISV(tipo?: string): string {
    if (!tipo) return 'ISV 15%';
    const t = tipo.trim().toUpperCase();
    if (t === 'ISV15') return 'ISV 15%';
    if (t === 'ISV18') return 'ISV 18%';
    if (t === 'EXENTO') return 'Exento';
    return tipo;
  }

  get filteredProducts() {
    const q = (this.searchProductos || this.search || '').trim().toLowerCase();
    if (!q) return this.products;
    return this.products.filter(p =>
      (p.nombre || '').toLowerCase().includes(q) ||
      (p.codigo || '').toLowerCase().includes(q) ||
      (p.codigoBarras || '').toLowerCase().includes(q) ||
      (p.descripcion || '').toLowerCase().includes(q) ||
      (p.tipoISV || '').toLowerCase().includes(q) ||
      String(p.precioVenta || p.precio || '').includes(q) ||
      String(p.stock || '').includes(q)
    );
  }

  get filteredClientes(): Cliente[] {
    const q = (this.searchClientes || '').trim().toLowerCase();
    if (!q) return this.clientes;
    return this.clientes.filter(c =>
      (c.nombre || '').toLowerCase().includes(q) ||
      (c.rtn || '').toLowerCase().includes(q) ||
      (c.telefono || '').toLowerCase().includes(q) ||
      (c.email || '').toLowerCase().includes(q) ||
      (c.direccion || '').toLowerCase().includes(q)
    );
  }

  get filteredProveedores(): Proveedor[] {
    const q = (this.searchProveedores || '').trim().toLowerCase();
    if (!q) return this.proveedores;
    return this.proveedores.filter(p =>
      (p.nombre || '').toLowerCase().includes(q) ||
      (p.contacto || '').toLowerCase().includes(q) ||
      (p.rtn || '').toLowerCase().includes(q) ||
      (p.telefono || '').toLowerCase().includes(q) ||
      (p.email || '').toLowerCase().includes(q)
    );
  }

  get filteredCompras(): Compra[] {
    const q = (this.searchCompras || '').trim().toLowerCase();
    if (!q) return this.compras;
    const cleanId = q.replace('#oc-', '').replace('oc-', '').replace('#', '');
    return this.compras.filter(c =>
      String(c.compraId || '').toLowerCase().includes(cleanId) ||
      (c.proveedor || '').toLowerCase().includes(q) ||
      (c.estadoCompra || '').toLowerCase().includes(q) ||
      String(c.total || '').includes(q) ||
      (c.fechaCompra ? String(c.fechaCompra).toLowerCase().includes(q) : false)
    );
  }

  get filteredVentas() {
    return this.ventas.filter(v => {
      // 1. Filtro ID
      if (this.filtroVentas.id && this.filtroVentas.id.trim()) {
        const idSearch = this.filtroVentas.id.trim().toLowerCase().replace('#v-', '').replace('v-', '').replace('#', '');
        const idStr = String(v.ventaId || '').toLowerCase();
        if (!idStr.includes(idSearch)) return false;
      }

      // 2. Filtro Tipo
      if (this.filtroVentas.tipo === 'FACTURA' && v.esCotizacion) return false;
      if (this.filtroVentas.tipo === 'COTIZACION' && !v.esCotizacion) return false;

      // 3. Filtro N° de Documento (Correlativo COT / SAR)
      if (this.filtroVentas.documento && this.filtroVentas.documento.trim()) {
        const docQ = this.filtroVentas.documento.trim().toLowerCase();
        const numCot = (v.numeroCotizacion || `cot-${v.ventaId}`).toLowerCase();
        const numFac = (v.numeroFactura || '').toLowerCase();
        if (!numCot.includes(docQ) && !numFac.includes(docQ)) return false;
      }

      // 4. Filtro Cliente
      if (this.filtroVentas.cliente && this.filtroVentas.cliente.trim()) {
        const cliQ = this.filtroVentas.cliente.trim().toLowerCase();
        const cliNombre = (v.cliente || '').toLowerCase();
        const cliRtn = (v.rtn || '').toLowerCase();
        if (!cliNombre.includes(cliQ) && !cliRtn.includes(cliQ)) return false;
      }

      // 5. Filtro Fecha
      if (this.filtroVentas.fecha && this.filtroVentas.fecha.trim()) {
        const fQ = this.filtroVentas.fecha.trim();
        const rawDate = v.fechaVenta ? String(v.fechaVenta) : '';
        const localDate = v.fechaVenta ? new Date(v.fechaVenta).toISOString().substring(0, 10) : '';
        if (!rawDate.includes(fQ) && !localDate.includes(fQ)) return false;
      }

      // 6. Filtro CAI
      if (this.filtroVentas.cai === 'CON_CAI') {
        if (v.esCotizacion || !v.cai) return false;
      } else if (this.filtroVentas.cai === 'SIN_CAI') {
        if (!v.esCotizacion && v.cai) return false;
      }

      // 7. Filtro Monto / Total Mínimo
      if (this.filtroVentas.montoMin !== null && this.filtroVentas.montoMin !== undefined && this.filtroVentas.montoMin > 0) {
        if ((v.total || 0) < this.filtroVentas.montoMin) return false;
      }

      // 8. Filtro Estado
      if (this.filtroVentas.estado && this.filtroVentas.estado !== 'TODOS') {
        const estQ = this.filtroVentas.estado.toLowerCase();
        if (estQ.includes('anula')) {
          if (!this.esVentaAnulada(v)) return false;
        } else {
          if (this.esVentaAnulada(v)) return false;
          const estVal = (v.estadoVenta || '').toLowerCase();
          if (!estVal.includes(estQ)) return false;
        }
      }

      // Filtro global opcional por si se usa search global
      if (this.search && this.search.trim()) {
        const globalQ = this.search.trim().toLowerCase();
        const full = `${v.cliente} ${v.numeroFactura || ''} ${v.numeroCotizacion || ''} ${v.cai || ''}`.toLowerCase();
        if (!full.includes(globalQ)) return false;
      }

      return true;
    });
  }

  get recentSales() {
    return this.ventas.slice(0, 5);
  }

  // =========================================================================
  // ALERTAS PREVENTIVAS DE CAI Y RANGO SAR
  // =========================================================================
  get alertaCAI(): { tipo: 'danger' | 'warning'; titulo: string; mensaje: string; detalle: string; diasRestantes: number; facturasDisponibles: number } | null {
    // Solo visible para administradores y superusuarios (oculto para usuarios con rol de facturación/ventas)
    if (!this.isAdmin) return null;
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
      const matchFecha = (f.getMonth() + 1) === Number(this.reporteMesVentas) && f.getFullYear() === Number(this.reporteAnoVentas);
      if (!matchFecha) return false;

      if (this.searchReporteVentas && this.searchReporteVentas.trim()) {
        const q = this.searchReporteVentas.trim().toLowerCase();
        const numFact = (v.numeroFactura || '').toLowerCase();
        const cli = (v.cliente || '').toLowerCase();
        const rtn = (v.clienteRtn || v.rtn || '').toLowerCase();
        const cai = (v.cai || '').toLowerCase();
        if (!numFact.includes(q) && !cli.includes(q) && !rtn.includes(q) && !cai.includes(q)) {
          return false;
        }
      }

      return true;
    });
  }

  get inventarioValorizadoFiltrado(): any[] {
    const items = this.inventarioValorizado;
    if (!this.searchReporteInventario || !this.searchReporteInventario.trim()) return items;
    const q = this.searchReporteInventario.trim().toLowerCase();
    return items.filter(i =>
      (i.codigo || '').toLowerCase().includes(q) ||
      (i.nombre || '').toLowerCase().includes(q)
    );
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

  get logsAuditoriaFiltrados(): any[] {
    let list = this.logsAuditoriaList || [];
    if (this.filtroTipoAuditoria && this.filtroTipoAuditoria !== 'TODOS') {
      const ft = this.filtroTipoAuditoria.toUpperCase();
      list = list.filter(item => (item.tipo || '').toUpperCase().includes(ft));
    }
    if (this.busquedaAuditoria && this.busquedaAuditoria.trim()) {
      const q = this.busquedaAuditoria.trim().toLowerCase();
      list = list.filter(item =>
        (item.referencia || '').toLowerCase().includes(q) ||
        (item.usuario || '').toLowerCase().includes(q) ||
        (item.accion || '').toLowerCase().includes(q) ||
        (item.detalle || '').toLowerCase().includes(q) ||
        (item.valorAnterior || '').toLowerCase().includes(q) ||
        (item.valorNuevo || '').toLowerCase().includes(q)
      );
    }
    return list;
  }

  loadAuditoriaLogs() {
    if (!this.isSuperUsuario) return;
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

  abrirHistorialEntidad(tipo: string, entidadId?: number, referencia?: string) {
    if (!this.isSuperUsuario) return;
    this.showHistorialEntidadModal = true;
    this.cargandoHistorialEntidad = true;
    this.historialEntidadTipo = tipo;
    this.historialEntidadList = [];

    const ref = referencia || (entidadId ? `#${entidadId}` : '');
    this.historialEntidadTitulo = `Historial de Auditoría — ${tipo} ${ref}`;
    this.historialEntidadSubtitulo = `Registro cronológico de todas las modificaciones, valores anteriores y nuevos.`;

    const params = `tipo=${encodeURIComponent(tipo)}&entidadId=${entidadId || ''}&referencia=${encodeURIComponent(referencia || '')}`;
    this.http.get<any[]>(`${this.api}/Auditoria/historial?${params}`, this.getHeaders()).subscribe({
      next: (data) => {
        this.historialEntidadList = Array.isArray(data) ? data : [];
        this.cargandoHistorialEntidad = false;
      },
      error: (err) => {
        this.cargandoHistorialEntidad = false;
        const local = (this.logsAuditoriaList || []).filter(l =>
          (entidadId && l.entidadId === entidadId) || (referencia && l.referencia && l.referencia.includes(referencia))
        );
        if (local.length > 0) {
          this.historialEntidadList = local;
        } else if (!this.handleAuthError(err)) {
          this.notificarError('Error en Historial', err.error?.mensaje || 'No se pudo cargar el historial de la entidad.');
        }
      }
    });
  }

  cerrarHistorialEntidad() {
    this.showHistorialEntidadModal = false;
  }

  getFechaTime(item: any): number {
    if (!item) return 0;
    const val = item.fechaModificacion || item.fecha || item.fechaEmision || item.fechaApertura || item.fechaCompra || item.fechaVenta;
    if (!val) return 0;
    const parsed = this.parseFechaUtc(val);
    return parsed ? parsed.getTime() : 0;
  }

  parseFechaUtc(dateVal: any): Date | null {
    if (!dateVal) return null;
    if (dateVal instanceof Date) return dateVal;
    let str = String(dateVal).trim();
    if (/^\d{4}-\d{2}-\d{2}$/.test(str)) {
      return new Date(str + 'T12:00:00Z');
    }
    if (/^\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}/.test(str)) {
      str = str.replace(' ', 'T');
    }
    if (!str.endsWith('Z') && !str.includes('+') && !/(-\d{2}:\d{2})$/.test(str)) {
      str += 'Z';
    }
    const d = new Date(str);
    return isNaN(d.getTime()) ? null : d;
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
    if (!this.isAdmin) {
      this.notificarAdvertencia('Acceso denegado', 'Esta acción está restringida únicamente para administradores.');
      return;
    }
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
    if (!this.isAdmin) {
      this.notificarAdvertencia('Acceso denegado', 'Esta acción está restringida únicamente para administradores.');
      return;
    }
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

  // =========================================================================
  // MÉTODOS DE IMPORTACIÓN Y EXPORTACIÓN DE TABLAS
  // =========================================================================
  abrirModalImportar(tipo: 'Productos' | 'Clientes' | 'Proveedores') {
    if (!this.isAdmin) {
      this.notificarAdvertencia('Acceso denegado', 'Esta acción está restringida únicamente para administradores.');
      return;
    }
    this.importModalTipo = tipo;
    this.registrosImportarPreview = [];
    this.columnasPreview = [];
    this.archivoSeleccionadoNombre = '';
    this.showImportModal = true;
  }

  cerrarModalImportar() {
    this.showImportModal = false;
    this.registrosImportarPreview = [];
    this.columnasPreview = [];
    this.archivoSeleccionadoNombre = '';
  }

  descargarPlantillaExcel(tipo: 'Productos' | 'Clientes' | 'Proveedores') {
    let data: any[] = [];
    let nombreArchivo = '';

    if (tipo === 'Productos') {
      nombreArchivo = 'Plantilla_Importar_Productos_VisionTech.xlsx';
      data = [
        {
          'Nombre': 'Lente Oftálmico Antirreflejo Blue Protect',
          'CodigoBarras': 'OPT-7421001',
          'Precio': 850.00,
          'StockInicial': 25,
          'TipoISV': 'ISV15',
          'Descripcion': 'Cristales orgánicos con tratamiento antirreflejante y filtro luz azul'
        },
        {
          'Nombre': 'Armazón Oftálmico Titanium Flex',
          'CodigoBarras': 'OPT-7421002',
          'Precio': 1450.00,
          'StockInicial': 15,
          'TipoISV': 'ISV15',
          'Descripcion': 'Montura metálica ultraligera para caballero'
        }
      ];
    } else if (tipo === 'Clientes') {
      nombreArchivo = 'Plantilla_Importar_Clientes_VisionTech.xlsx';
      data = [
        {
          'Nombre': 'Juan Carlos Rodriguez',
          'RTN': '0801-1988-12345',
          'Telefono': '9876-5432',
          'Email': 'juan.rodriguez@gmail.com',
          'Direccion': 'Tegucigalpa, Col. Kennedy Bloque 12 Casa 4',
          'TipoCliente': 'Natural'
        },
        {
          'Nombre': 'Inversiones Médicas de Honduras S.A.',
          'RTN': '0801-9002-345678',
          'Telefono': '2235-8899',
          'Email': 'compras@inversionesmedicas.hn',
          'Direccion': 'San Pedro Sula, Blvd. Morazán',
          'TipoCliente': 'Juridico'
        }
      ];
    } else if (tipo === 'Proveedores') {
      nombreArchivo = 'Plantilla_Importar_Proveedores_VisionTech.xlsx';
      data = [
        {
          'Nombre': 'Distribuidora Óptica Visión S.A.',
          'RTN': '0801-9003-456789',
          'Contacto': 'Lic. Laura Reyes',
          'Telefono': '2234-5678',
          'Email': 'contacto@distribuidoravision.hn',
          'Direccion': 'San Pedro Sula, Barrio Guamilito 5ta Ave.'
        },
        {
          'Nombre': 'Lentes y Cristales de Centroamérica',
          'RTN': '0801-9010-887766',
          'Contacto': 'Ing. Roberto Paz',
          'Telefono': '9944-1122',
          'Email': 'ventas@cristalesca.com',
          'Direccion': 'Tegucigalpa, Parque Empresarial Las Hadas'
        }
      ];
    }

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, `Plantilla_${tipo}`);
    XLSX.writeFile(wb, nombreArchivo);
    this.notificarInfo('Plantilla Descargada', `Se ha generado la plantilla Excel de ${tipo}. Llénala con tus registros e impórtala.`);
  }

  onArchivoImportarSeleccionado(event: any) {
    const file = event.target.files?.[0];
    if (!file) return;

    this.archivoSeleccionadoNombre = file.name;
    const reader = new FileReader();

    reader.onload = (e: any) => {
      try {
        const bstr = e.target.result;
        const wb = XLSX.read(bstr, { type: 'binary' });
        const wsname = wb.SheetNames[0];
        const ws = wb.Sheets[wsname];
        const rawJson: any[] = XLSX.utils.sheet_to_json(ws);

        if (!rawJson || rawJson.length === 0) {
          this.notificarAdvertencia('Archivo Vacío', 'No se encontraron filas con datos en el archivo seleccionado.');
          this.registrosImportarPreview = [];
          this.columnasPreview = [];
          return;
        }

        if (this.importModalTipo === 'Productos') {
          this.registrosImportarPreview = rawJson.map(row => {
            const getVal = (...keys: string[]) => {
              for (const k of keys) {
                const foundKey = Object.keys(row).find(x => x.trim().toLowerCase() === k.toLowerCase());
                if (foundKey && row[foundKey] !== undefined) return row[foundKey];
              }
              return undefined;
            };

            const nombre = String(getVal('Nombre', 'Producto', 'DescripcionProducto', 'Item') || '').trim();
            const precio = parseFloat(String(getVal('Precio', 'PrecioVenta', 'Precio Venta', 'Valor') || '0')) || 0;
            const stock = parseInt(String(getVal('StockInicial', 'Stock', 'Cantidad', 'Stock Inicial', 'Existencias') || '0'), 10) || 0;
            const codigo = String(getVal('CodigoBarras', 'Codigo', 'Código', 'SKU', 'Codigo Barras') || '').trim();
            const tipoISV = String(getVal('TipoISV', 'ISV', 'Impuesto') || 'ISV15').trim();
            const desc = String(getVal('Descripcion', 'Descripción', 'Detalle') || '').trim();

            return {
              nombre,
              precio,
              stockInicial: stock,
              codigoBarras: codigo || null,
              tipoISV: tipoISV || 'ISV15',
              descripcion: desc
            };
          }).filter(p => !!p.nombre);
          this.columnasPreview = ['Nombre', 'Código/SKU', 'Precio (L.)', 'Stock Inicial', 'ISV', 'Descripción'];
        } else if (this.importModalTipo === 'Clientes') {
          this.registrosImportarPreview = rawJson.map(row => {
            const getVal = (...keys: string[]) => {
              for (const k of keys) {
                const foundKey = Object.keys(row).find(x => x.trim().toLowerCase() === k.toLowerCase());
                if (foundKey && row[foundKey] !== undefined) return row[foundKey];
              }
              return undefined;
            };

            const nombre = String(getVal('Nombre', 'Cliente', 'Nombre Completo', 'Razon Social') || '').trim();
            const rtn = String(getVal('RTN', 'Rtn', 'Identidad', 'DNI') || '').trim();
            const telefono = String(getVal('Telefono', 'Teléfono', 'Celular', 'Movil') || '').trim();
            const email = String(getVal('Email', 'Correo', 'Correo Electrónico') || '').trim();
            const direccion = String(getVal('Direccion', 'Dirección', 'Ubicacion') || '').trim();
            const tipoCliente = String(getVal('TipoCliente', 'Tipo Cliente', 'Tipo') || 'Natural').trim();

            return {
              nombre,
              rtn: rtn || null,
              telefono,
              email,
              direccion,
              tipoCliente: tipoCliente || 'Natural'
            };
          }).filter(c => !!c.nombre);
          this.columnasPreview = ['Nombre', 'RTN', 'Teléfono', 'Email', 'Dirección', 'Tipo'];
        } else if (this.importModalTipo === 'Proveedores') {
          this.registrosImportarPreview = rawJson.map(row => {
            const getVal = (...keys: string[]) => {
              for (const k of keys) {
                const foundKey = Object.keys(row).find(x => x.trim().toLowerCase() === k.toLowerCase());
                if (foundKey && row[foundKey] !== undefined) return row[foundKey];
              }
              return undefined;
            };

            const nombre = String(getVal('Nombre', 'Proveedor', 'Empresa', 'Razon Social') || '').trim();
            const rtn = String(getVal('RTN', 'Rtn') || '').trim();
            const contacto = String(getVal('Contacto', 'Persona Contacto', 'Atencion') || '').trim();
            const telefono = String(getVal('Telefono', 'Teléfono', 'Celular') || '').trim();
            const email = String(getVal('Email', 'Correo', 'Correo Electrónico') || '').trim();
            const direccion = String(getVal('Direccion', 'Dirección', 'Ubicacion') || '').trim();

            return {
              nombre,
              rtn: rtn || null,
              contacto,
              telefono,
              email,
              direccion
            };
          }).filter(pr => !!pr.nombre);
          this.columnasPreview = ['Empresa', 'RTN', 'Contacto', 'Teléfono', 'Email', 'Dirección'];
        }

        if (this.registrosImportarPreview.length === 0) {
          this.notificarAdvertencia('Sin registros válidos', 'No se pudieron extraer registros con nombre válido del archivo.');
        } else {
          this.notificarInfo('Vista Previa Lista', `Se detectaron ${this.registrosImportarPreview.length} registros listos para importar.`);
        }
      } catch (err: any) {
        this.notificarError('Error de Lectura', 'No se pudo leer el archivo Excel/CSV. Verifique el formato.');
      }
    };

    reader.readAsBinaryString(file);
  }

  procesarImportacion() {
    if (!this.isAdmin) {
      this.notificarAdvertencia('Acceso denegado', 'Esta acción está restringida únicamente para administradores.');
      return;
    }
    if (!this.registrosImportarPreview || this.registrosImportarPreview.length === 0) {
      this.notificarAdvertencia('Sin registros', 'Seleccione un archivo con registros para importar.');
      return;
    }

    this.importandoDatos = true;
    const url = `${this.api}/${this.importModalTipo}/Importar`;

    this.http.post<any>(url, this.registrosImportarPreview, this.getHeaders()).subscribe({
      next: (res) => {
        this.importandoDatos = false;
        this.showImportModal = false;
        const insertados = res?.insertados ?? this.registrosImportarPreview.length;
        const omitidos = res?.omitidos ?? 0;
        this.notificarExito('Importación Exitosa', `Se procesaron los datos: ${insertados} registrados, ${omitidos} omitidos por duplicidad o validación.`);

        if (this.importModalTipo === 'Productos') {
          this.loadProducts();
          this.loadInventory();
        } else if (this.importModalTipo === 'Clientes') {
          this.loadClientes();
        } else if (this.importModalTipo === 'Proveedores') {
          this.loadProveedores();
        }
        this.registrosImportarPreview = [];
        this.archivoSeleccionadoNombre = '';
      },
      error: (err) => {
        this.importandoDatos = false;
        if (!this.handleAuthError(err)) {
          this.notificarError('Error en Importación', err.error?.mensaje || 'Ocurrió un error al guardar los registros en el sistema.');
        }
      }
    });
  }

  exportarProductosExcel() {
    if (!this.isAdmin) {
      this.notificarAdvertencia('Acceso denegado', 'Esta acción está restringida únicamente para administradores.');
      return;
    }
    const data = (this.filteredProducts || this.products).map((p, idx) => ({
      'No.': idx + 1,
      'ID': p.productoId,
      'Código / SKU': p.codigo || p.codigoBarras || '-',
      'Nombre': p.nombre,
      'Descripción': p.descripcion || '-',
      'Precio Venta (L.)': Number((p.precioVenta ?? p.precio ?? 0).toFixed(2)),
      'Stock Actual': p.stock ?? 0,
      'Tipo ISV': p.tipoISV || 'ISV15',
      'Estado': p.activo !== false ? 'Activo' : 'Inactivo'
    }));

    if (data.length === 0) {
      this.notificarAdvertencia('Sin productos', 'No hay productos disponibles para exportar.');
      return;
    }

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Productos');
    XLSX.writeFile(wb, `Catalogo_Productos_VisionTech_${new Date().toISOString().split('T')[0]}.xlsx`);
    this.notificarExito('Productos Exportados', 'El catálogo de productos se ha descargado en formato Excel.');
  }

  exportarClientesExcel() {
    if (!this.isAdmin) {
      this.notificarAdvertencia('Acceso denegado', 'Esta acción está restringida únicamente para administradores.');
      return;
    }
    const data = (this.filteredClientes || this.clientes).map((c, idx) => ({
      'No.': idx + 1,
      'ID': c.clienteId,
      'Nombre Completo': c.nombre,
      'RTN Fiscal': c.rtn || 'Consumidor Final',
      'Teléfono': c.telefono || '-',
      'Correo Electrónico': c.email || '-',
      'Dirección': c.direccion || '-',
      'Tipo Cliente': c.tipoCliente || 'Natural',
      'Estado': c.activo !== false ? 'Activo' : 'Inactivo'
    }));

    if (data.length === 0) {
      this.notificarAdvertencia('Sin clientes', 'No hay clientes disponibles para exportar.');
      return;
    }

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Clientes');
    XLSX.writeFile(wb, `Directorio_Clientes_VisionTech_${new Date().toISOString().split('T')[0]}.xlsx`);
    this.notificarExito('Clientes Exportados', 'El directorio de clientes se ha descargado en formato Excel.');
  }

  exportarProveedoresExcel() {
    if (!this.isAdmin) {
      this.notificarAdvertencia('Acceso denegado', 'Esta acción está restringida únicamente para administradores.');
      return;
    }
    const data = (this.filteredProveedores || this.proveedores).map((pr, idx) => ({
      'No.': idx + 1,
      'ID': pr.proveedorId,
      'Empresa / Proveedor': pr.nombre,
      'RTN Fiscal': pr.rtn || '-',
      'Persona Contacto': pr.contacto || '-',
      'Teléfono': pr.telefono || '-',
      'Correo Electrónico': pr.email || '-',
      'Dirección': pr.direccion || '-',
      'Estado': pr.activo !== false ? 'Activo' : 'Inactivo'
    }));

    if (data.length === 0) {
      this.notificarAdvertencia('Sin proveedores', 'No hay proveedores disponibles para exportar.');
      return;
    }

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Proveedores');
    XLSX.writeFile(wb, `Directorio_Proveedores_VisionTech_${new Date().toISOString().split('T')[0]}.xlsx`);
    this.notificarExito('Proveedores Exportados', 'El directorio de proveedores se ha descargado en formato Excel.');
  }

  exportarFacturasExcel() {
    if (!this.isAdmin) {
      this.notificarAdvertencia('Acceso denegado', 'Esta acción está restringida únicamente para administradores.');
      return;
    }
    const data = (this.filteredVentas || this.ventas).map((v, idx) => ({
      'No.': idx + 1,
      'ID Venta': v.ventaId,
      'Tipo Documento': v.esCotizacion || v.estadoVenta === 'Cotizacion' ? 'Cotización' : 'Factura Fiscal',
      'No. Documento': v.numeroFactura || v.numeroCotizacion || (`COT-` + v.ventaId),
      'Cliente': v.cliente,
      'RTN Cliente': v.clienteRtn || '0000-0000-00000',
      'CAI': v.cai || '-',
      'Fecha': new Date(v.fechaFactura || v.fechaVenta).toLocaleDateString('es-HN'),
      'Total (L.)': Number((v.total || 0).toFixed(2)),
      'Estado': v.estadoVenta
    }));

    if (data.length === 0) {
      this.notificarAdvertencia('Sin ventas', 'No hay registros de facturas/ventas para exportar.');
      return;
    }

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Facturas_Ventas');
    XLSX.writeFile(wb, `Historial_Facturas_VisionTech_${new Date().toISOString().split('T')[0]}.xlsx`);
    this.notificarExito('Facturas Exportadas', 'El historial de facturas y ventas se ha descargado en formato Excel.');
  }

  exportarComprasExcel() {
    if (!this.isAdmin) {
      this.notificarAdvertencia('Acceso denegado', 'Esta acción está restringida únicamente para administradores.');
      return;
    }
    const data = (this.filteredCompras || this.compras).map((c, idx) => ({
      'No.': idx + 1,
      'ID Orden (#OC)': c.compraId,
      'Proveedor': c.proveedor,
      'Fecha Registro': new Date(c.fechaCompra).toLocaleDateString('es-HN'),
      'Total (L.)': Number((c.total || 0).toFixed(2)),
      'Estado': c.estadoCompra
    }));

    if (data.length === 0) {
      this.notificarAdvertencia('Sin compras', 'No hay órdenes de compra registradas para exportar.');
      return;
    }

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Compras');
    XLSX.writeFile(wb, `Ordenes_Compra_VisionTech_${new Date().toISOString().split('T')[0]}.xlsx`);
    this.notificarExito('Compras Exportadas', 'Las órdenes de compra se han descargado en formato Excel.');
  }
}
