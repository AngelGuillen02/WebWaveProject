export const environment = {
  production: true,
  // Si tienes la URL pública de tu API desplegada, cámbiala aquí (ejemplo: 'https://tu-api.up.railway.app/api')
  // O déjala que tome automáticamente del origen si hay reverse proxy
  apiUrl: (typeof window !== 'undefined' && (window as any).__API_URL__)
    ? (window as any).__API_URL__
    : 'https://sistemavisiontech-api.onrender.com/api'
};
