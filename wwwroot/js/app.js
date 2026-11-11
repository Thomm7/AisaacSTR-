/* =============================================================================
   AISAAC STR — SISTEMA DE GESTIÓN RETAIL (POS & ERP)
   Lógica de Cliente: Autenticación JWT, RBAC, POS, Cierre, Inventario, Compras y Dashboard
   ============================================================================= */

// ── Estado Global ─────────────────────────────────────────────────────────────
const state = {
  usuario: null,
  token: null,
  catalogoVariantes: [],
  categoriaActivaPOS: '',
  carrito: [],
  metodoPagoSeleccionado: null,
  cierreData: null
};

// ── Formateadores ─────────────────────────────────────────────────────────────
function fmt(n) {
  if (isNaN(n) || n === null || n === undefined) return '$ 0';
  return '$ ' + Math.round(Number(n)).toLocaleString('es-CO');
}

function fmtDate(d) {
  const date = d ? new Date(d) : new Date();
  return date.toLocaleDateString('es-CO', {
    weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
  }).toUpperCase();
}

function fmtDateTime(d) {
  const date = d ? new Date(d) : new Date();
  return date.toLocaleDateString('es-CO') + ' ' +
    date.toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit', hour12: true });
}

// ── Notificaciones Toast ──────────────────────────────────────────────────────
function toast(msg, type = 'success') {
  const container = document.getElementById('toast-container');
  if (!container) return;
  const el = document.createElement('div');
  el.className = `toast ${type}`;
  el.innerHTML = `<span class="toast-icon">${type === 'success' ? '✓' : '⚠'}</span><span>${msg}</span>`;
  container.appendChild(el);
  setTimeout(() => el.remove(), 4000);
}

// ── Cliente HTTP con Autenticación JWT ────────────────────────────────────────
async function fetchWithAuth(url, options = {}) {
  const headers = options.headers || {};
  if (state.token) {
    headers['Authorization'] = `Bearer ${state.token}`;
  }
  if (options.body && typeof options.body === 'object' && !(options.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json';
    options.body = JSON.stringify(options.body);
  }
  options.headers = headers;

  const res = await fetch(url, options);
  if (res.status === 401) {
    toast('Sesión expirada o no autorizada. Por favor inicia sesión.', 'error');
    logout();
    throw new Error('No autorizado');
  }
  if (res.status === 403) {
    toast('Acceso denegado: Tu rol de usuario no tiene permisos para esta acción.', 'error');
    throw new Error('Prohibido por RBAC');
  }
  return res;
}

// ═════════════════════════════════════════════════════════════════════════════
// 1. MÓDULO DE AUTENTICACIÓN Y CONTROL DE ACCESO (RBAC)
// ═════════════════════════════════════════════════════════════════════════════

function checkAuth() {
  const savedToken = localStorage.getItem('aisaac_token');
  const savedUser = localStorage.getItem('aisaac_user');

  if (savedToken && savedUser) {
    try {
      state.token = savedToken;
      state.usuario = JSON.parse(savedUser);
      aplicarSesionUsuario();
      return;
    } catch {
      localStorage.removeItem('aisaac_token');
      localStorage.removeItem('aisaac_user');
    }
  }

  // Si no está autenticado, abrir overlay de login
  const overlay = document.getElementById('auth-overlay');
  if (overlay) {
    overlay.style.removeProperty('display');
    overlay.style.display = 'flex';
    overlay.classList.remove('hidden');
  }
  const infoBox = document.getElementById('user-info-box');
  if (infoBox) infoBox.style.display = 'none';
}

// Procesar formulario de login con correo y contraseña obligatorios

async function handleLogin(e) {
  if (e) {
    e.preventDefault();
    e.stopPropagation();
  }
  const email = document.getElementById('login-email')?.value?.trim();
  const password = document.getElementById('login-password')?.value;

  if (!email || !password) {
    toast('Por favor ingresa tu correo y contraseña', 'error');
    return false;
  }

  await submitLogin(email, password);
  return false;
}

async function submitLogin(email, password) {
  const submitBtn = document.getElementById('login-submit-btn');
  if (submitBtn) {
    submitBtn.disabled = true;
    submitBtn.textContent = 'Verificando credenciales...';
  }

  try {
    const res = await fetch('/api/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      },
      body: JSON.stringify({ email: email.trim(), password: password })
    });

    const data = await res.json();
    if (!res.ok) {
      toast(data.mensaje || 'Credenciales incorrectas', 'error');
      return false;
    }

    state.token = data.token;
    state.usuario = {
      id: data.usuarioId,
      nombre: data.nombreCompleto,
      email: data.email,
      rol: data.rol
    };

    localStorage.setItem('aisaac_token', state.token);
    localStorage.setItem('aisaac_user', JSON.stringify(state.usuario));

    toast(`¡Bienvenido(a), ${state.usuario.nombre}!`);
    aplicarSesionUsuario();
    return true;
  } catch (err) {
    console.error('Error de login:', err);
    toast('Error conectando al servidor de autenticación', 'error');
    return false;
  } finally {
    if (submitBtn) {
      submitBtn.disabled = false;
      submitBtn.innerHTML = `
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
          <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"/>
          <polyline points="10 17 15 12 10 7"/>
          <line x1="15" y1="12" x2="3" y2="12"/>
        </svg>
        Iniciar Sesión`;
    }
  }
}

function aplicarSesionUsuario() {
  const overlay = document.getElementById('auth-overlay');
  if (overlay) {
    overlay.classList.add('hidden');
    overlay.style.setProperty('display', 'none', 'important');
  }

  const infoBox = document.getElementById('user-info-box');
  if (infoBox) infoBox.style.display = 'flex';

  const nameEl = document.getElementById('user-name-display');
  if (nameEl && state.usuario) nameEl.textContent = state.usuario.nombre;

  const roleDisplay = document.getElementById('user-role-display');
  if (roleDisplay && state.usuario) {
    roleDisplay.textContent = state.usuario.rol.toUpperCase();
  }

  // Control RBAC estricto en la interfaz
  const esAdmin = state.usuario && state.usuario.rol === 'Administrador';
  if (esAdmin) {
    if (roleDisplay) roleDisplay.className = 'role-pill admin';
    document.querySelectorAll('.tab-btn.admin-only').forEach(el => {
      el.style.setProperty('display', 'flex', 'important');
    });
  } else {
    // Cajero / Empleado
    if (roleDisplay) roleDisplay.className = 'role-pill cajero';
    document.querySelectorAll('.tab-btn.admin-only').forEach(el => {
      el.style.setProperty('display', 'none', 'important');
    });
    // Si estaba en una pestaña de admin, mover al POS
    const tabPdv = document.getElementById('tab-pdv');
    if (tabPdv) tabPdv.click();
  }

  // Visibilidad del botón de carrito según panel actual
  actualizarVisibilidadCarrito(document.querySelector('.tab-btn.active')?.dataset?.panel || 'panel-pdv');

  // Cargar datos iniciales de forma segura
  try { cargarCatalogoVariantes(); } catch (e) { console.error(e); }
  try { 
    renderCierre().then(() => {
      // Si entra como Cajero y no hay caja abierta, abrir automáticamente el modal de «Abrir caja»
      if (state.usuario && state.usuario.rol === 'Cajero' && state.cierreData && state.cierreData.estadoCaja !== 'Abierta') {
        setTimeout(() => {
          abrirModalAperturaCaja();
        }, 350);
      }
    }); 
  } catch (e) { console.error(e); }
  if (esAdmin) {
    try { cargarInventario(); } catch (e) { console.error(e); }
    try { cargarProveedores(); } catch (e) { console.error(e); }
    try { cargarCompras(); } catch (e) { console.error(e); }
    try { cargarDashboard(); } catch (e) { console.error(e); }
    try { cargarUsuarios(); } catch (e) { console.error(e); }
  }
}

let usuariosGestion = [];

async function cargarUsuarios() {
  const container = document.getElementById('usuarios-container');
  if (!container) return;
  try {
    const res = await fetchWithAuth('/api/auth/usuarios');
    if (!res.ok) throw new Error('No se pudo cargar la lista de usuarios');
    usuariosGestion = await res.json();
    container.innerHTML = `
      <div style="overflow-x:auto;">
        <table class="data-table" style="width:100%;">
          <thead><tr><th>Nombre</th><th>Correo</th><th>Teléfono</th><th>Rol</th><th>Estado</th><th>Acciones</th></tr></thead>
          <tbody>${usuariosGestion.map(u => `
            <tr>
              <td>${u.nombreCompleto}</td>
              <td>${u.email}</td>
              <td>${u.telefono || '-'}</td>
              <td>${u.rol}</td>
              <td><span class="pill-stock ${u.activo ? 'in-stock' : 'out-stock'}">${u.activo ? 'Activo' : 'Inactivo'}</span></td>
              <td class="user-actions-cell">
                <div class="user-actions" style="display: flex; align-items: center; gap: 8px; flex-wrap: nowrap;">
                  <button type="button" class="btn-secondary btn-user-action" onclick="abrirModalEditarUsuario(${u.id})">Editar</button>
                  <button type="button" class="btn-secondary btn-user-action" onclick="abrirModalPasswordUsuario(${u.id})">Restablecer contraseña</button>
                  <button type="button" class="btn-secondary btn-user-action" onclick="cambiarEstadoUsuario(${u.id}, ${!u.activo})">${u.activo ? 'Desactivar' : 'Activar'}</button>
                </div>
              </td>
            </tr>`).join('')}
          </tbody>
        </table>
      </div>`;
  } catch (err) {
    container.innerHTML = `<div class="dashboard-empty">${err.message}</div>`;
  }
}

function abrirModalEditarUsuario(id) {
  const usuario = usuariosGestion.find(u => u.id === id);
  if (!usuario) return;
  document.getElementById('editar-usuario-id').value = usuario.id;
  document.getElementById('editar-usuario-nombre').value = usuario.nombreCompleto || '';
  document.getElementById('editar-usuario-email').value = usuario.email || '';
  document.getElementById('editar-usuario-telefono').value = usuario.telefono || '';
  document.getElementById('editar-usuario-rol').value = usuario.rol || 'Cajero';
  const modal = document.getElementById('modal-editar-usuario');
  modal.classList.remove('hidden');
  modal.setAttribute('aria-hidden', 'false');
  document.body.classList.add('modal-open');
  document.getElementById('editar-usuario-nombre').focus();
}

function cerrarModalEditarUsuario() {
  const modal = document.getElementById('modal-editar-usuario');
  modal.classList.add('hidden');
  modal.setAttribute('aria-hidden', 'true');
  document.body.classList.remove('modal-open');
  document.getElementById('form-editar-usuario').reset();
}

function abrirModalPasswordUsuario(id) {
  const usuario = usuariosGestion.find(u => u.id === id);
  if (!usuario) return;
  document.getElementById('password-usuario-id').value = usuario.id;
  document.getElementById('form-password-usuario').reset();
  document.getElementById('password-usuario-id').value = usuario.id;
  const modal = document.getElementById('modal-password-usuario');
  modal.classList.remove('hidden');
  modal.setAttribute('aria-hidden', 'false');
  document.body.classList.add('modal-open');
  document.getElementById('password-usuario-nueva').focus();
}

function cerrarModalPasswordUsuario() {
  const modal = document.getElementById('modal-password-usuario');
  modal.classList.add('hidden');
  modal.setAttribute('aria-hidden', 'true');
  document.body.classList.remove('modal-open');
  document.getElementById('form-password-usuario').reset();
}

async function restablecerPasswordUsuario(event) {
  event.preventDefault();
  const password = document.getElementById('password-usuario-nueva').value;
  const confirmacion = document.getElementById('password-usuario-confirmar').value;

  if (password !== confirmacion) {
    toast('Las contraseñas no coinciden.', 'error');
    return;
  }
  if (password.length < 8 || !/[a-z]/.test(password) || !/[A-Z]/.test(password) ||
      !/\d/.test(password) || !/[^A-Za-z0-9]/.test(password)) {
    toast('La contraseña debe tener 8 caracteres e incluir mayúscula, minúscula, número y símbolo.', 'error');
    return;
  }

  const id = Number(document.getElementById('password-usuario-id').value);
  const res = await fetchWithAuth(`/api/auth/usuarios/${id}/password`, {
    method: 'PATCH',
    body: { password, confirmarPassword: confirmacion }
  });
  const data = await res.json();
  if (!res.ok) {
    toast(data.mensaje || 'No se pudo restablecer la contraseña.', 'error');
    return;
  }

  toast('Contraseña restablecida correctamente.');
  cerrarModalPasswordUsuario();
}

async function actualizarUsuario(event) {
  event.preventDefault();
  const form = event.currentTarget;
  if (!form.reportValidity()) return;

  const id = Number(document.getElementById('editar-usuario-id').value);
  const nombre = document.getElementById('editar-usuario-nombre').value.trim();
  const email = document.getElementById('editar-usuario-email').value.trim();
  const telefono = document.getElementById('editar-usuario-telefono').value.trim();
  const rol = document.getElementById('editar-usuario-rol').value;

  if (!/^[A-Za-zÀ-ÿ\s]+$/.test(nombre)) {
    toast('El nombre completo solo permite letras y espacios.', 'error');
    document.getElementById('editar-usuario-nombre').focus();
    return;
  }

  if (telefono && !/^[0-9+\s]+$/.test(telefono)) {
    toast('El teléfono solo permite números, signos más y espacios.', 'error');
    document.getElementById('editar-usuario-telefono').focus();
    return;
  }

  const payload = {
    nombreCompleto: nombre,
    email: email,
    telefono: telefono,
    rol: rol
  };
  const res = await fetchWithAuth(`/api/auth/usuarios/${id}`, { method: 'PUT', body: payload });
  const data = await res.json();
  if (!res.ok) {
    toast(data.mensaje || 'No se pudo actualizar el usuario.', 'error');
    return;
  }

  toast('Usuario actualizado correctamente.');
  cerrarModalEditarUsuario();
  cargarUsuarios();
}



function abrirFormularioUsuario() {
  const form = document.getElementById('form-crear-usuario');
  if (form) {
    form.style.display = 'grid';
    document.getElementById('usuario-nombre')?.focus();
  }
}

function cancelarFormularioUsuario() {
  const form = document.getElementById('form-crear-usuario');
  if (!form) return;
  form.reset();
  form.style.display = 'none';
}

async function crearUsuario(event) {
  event.preventDefault();
  const form = event.currentTarget;
  if (!form.reportValidity()) return;
  const passwordInput = document.getElementById('usuario-password');
  const password = passwordInput.value;
  if (!/[a-z]/.test(password) || !/[A-Z]/.test(password) ||
      !/\d/.test(password) || !/[^A-Za-z0-9]/.test(password)) {
    passwordInput.setCustomValidity('La contraseña debe incluir mayúscula, minúscula, número y carácter especial.');
    form.reportValidity();
    return;
  }
  passwordInput.setCustomValidity('');
  const payload = {
    nombreCompleto: document.getElementById('usuario-nombre').value.trim(),
    email: document.getElementById('usuario-email').value.trim(),
    password: document.getElementById('usuario-password').value,
    telefono: document.getElementById('usuario-telefono').value.trim(),
    rol: document.getElementById('usuario-rol').value
  };

  const res = await fetchWithAuth('/api/auth/usuarios', {
    method: 'POST',
    body: payload
  });
  const data = await res.json();
  if (!res.ok) {
    toast(data.mensaje || 'No se pudo crear el usuario.', 'error');
    return;
  }

  toast(`Usuario ${data.nombreCompleto} creado correctamente.`);
  cancelarFormularioUsuario();
  cargarUsuarios();
}

async function cambiarEstadoUsuario(id, activo) {
  const accion = activo ? 'activar' : 'desactivar';
  if (!window.confirm(`¿Seguro que deseas ${accion} esta cuenta de usuario?`)) return;
  const res = await fetchWithAuth(`/api/auth/usuarios/${id}/estado`, {
    method: 'PATCH',
    body: { activo }
  });
  const data = await res.json();
  if (!res.ok) {
    toast(data.mensaje || 'No se pudo actualizar el usuario.', 'error');
    return;
  }
  toast(activo ? 'Usuario activado.' : 'Usuario desactivado.');
  cargarUsuarios();
}

function logout() {
  // 1. Limpiar almacenamiento de sesión y cookies
  localStorage.removeItem('aisaac_token');
  localStorage.removeItem('aisaac_user');
  sessionStorage.clear();
  document.cookie = "Aisaac_Token=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT";

  // 2. Limpiar estado en memoria
  state.usuario = null;
  state.token = null;
  state.carrito = [];
  state.metodoPagoSeleccionado = null;
  state.cierreData = null;

  // 3. Renderizar carrito vacío en la interfaz
  renderCarrito();

  // 4. Limpiar campos del formulario de login
  const emailInput = document.getElementById('login-email');
  const passInput = document.getElementById('login-password');
  if (emailInput) emailInput.value = '';
  if (passInput) passInput.value = '';

  // 5. Ocultar badges de usuario
  const infoBox = document.getElementById('user-info-box');
  if (infoBox) infoBox.style.display = 'none';

  // 6. Ocultar todos los módulos restringidos de Administrador (RBAC)
  document.querySelectorAll('.tab-btn.admin-only').forEach(el => {
    el.style.setProperty('display', 'none', 'important');
  });

  // 7. Resetear navegación de pestañas al Punto de Venta
  document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
  document.querySelectorAll('.panel').forEach(p => p.classList.remove('active'));
  const tabPdv = document.getElementById('tab-pdv');
  const panelPdv = document.getElementById('panel-pdv');
  if (tabPdv) tabPdv.classList.add('active');
  if (panelPdv) panelPdv.classList.add('active');

  // 8. Cerrar cualquier modal abierto
  document.querySelectorAll('.modal-overlay').forEach(m => m.classList.add('hidden'));

  // 9. Mostrar modal de autenticación de forma garantizada
  const overlay = document.getElementById('auth-overlay');
  if (overlay) {
    overlay.style.removeProperty('display');
    overlay.style.display = 'flex';
    overlay.classList.remove('hidden');
  }

  // 10. Enfocar input de email para facilitar nuevo inicio
  if (emailInput) {
    setTimeout(() => emailInput.focus(), 150);
  }

  toast('Sesión cerrada correctamente. Ingrese con sus credenciales.');
}

// ═════════════════════════════════════════════════════════════════════════════
// 2. PUNTO DE VENTA (POS)
// ═════════════════════════════════════════════════════════════════════════════

async function cargarCatalogoVariantes() {
  try {
    const res = await fetchWithAuth('/api/inventario/variantes');
    if (res.ok) {
      state.catalogoVariantes = await res.json();
      actualizarSelectsFiltros();
      renderCatalogoPOS();
      // Si el carrito está vacío, poblar demo amigable inicial
      if (state.carrito.length === 0 && state.catalogoVariantes.length > 0) {
        const item1 = state.catalogoVariantes.find(v => v.sku === 'CAM-001') || state.catalogoVariantes[0];
        if (item1 && item1.stockActual > 0) {
          state.carrito.push({
            varianteId: item1.id,
            sku: item1.sku,
            producto: item1.nombreProducto,
            variante: `${item1.talla} / ${item1.color} (${item1.marca})`,
            precio: item1.precioVenta,
            cantidad: 1,
            stockMax: item1.stockActual
          });
        }
      }
      renderCarrito();
    }
  } catch (err) {
    console.error('Error cargando catálogo:', err);
  }
}

function actualizarSelectsFiltros() {
  // Llenar combo de variantes en modal de compras
  const selectCompra = document.getElementById('compra-variante-select');
  if (selectCompra) {
    selectCompra.innerHTML = state.catalogoVariantes.map(v => `
      <option value="${v.id}" data-costo="${v.precioCompra}">
        ${v.sku} - ${v.nombreProducto} [${v.talla}/${v.color}] (Stock: ${v.stockActual})
      </option>
    `).join('');
    autoCompletarCostoCompra();
  }
}

function renderCarrito() {
  const container = document.getElementById('cart-body');
  const countBadge = document.getElementById('cart-badge-count');
  const topbarBadge = document.getElementById('topbar-cart-count');

  const totalPrendas = state.carrito.reduce((acc, i) => acc + i.cantidad, 0);
  if (countBadge) {
    countBadge.textContent = `${totalPrendas} prenda(s)`;
  }
  if (topbarBadge) {
    topbarBadge.textContent = totalPrendas;
  }

  if (!container) return;

  if (state.carrito.length === 0) {
    container.innerHTML = `
      <div class="empty-cart-box">
        <span>🛍️</span>
        <span style="font-weight:600; color:#ddd;">Tu orden está vacía</span>
        <span style="font-size:11px; color:var(--text-muted);">Haz clic en "+ AGREGAR" en cualquier prenda del catálogo</span>
      </div>
    `;
    updateSummary();
    return;
  }

  container.innerHTML = state.carrito.map((item, i) => `
    <div class="cart-card-item">
      <div class="cart-item-info">
        <div class="cart-item-title" title="${item.producto}">${item.producto}</div>
        <div class="cart-item-sub">
          <span class="cart-item-sku">${item.sku}</span>
          <span>·</span>
          <span>${item.variante}</span>
        </div>
        <div class="cart-item-pricing">
          <span>Cantidad <strong>${item.cantidad}</strong></span>
          <span>${fmt(item.precio)} c/u</span>
          <span class="cart-item-price-tag">${fmt(item.precio * item.cantidad)}</span>
        </div>
      </div>
      <div class="cart-item-qty">
        <button type="button" class="qty-btn" onclick="changeQty(${i}, -1)" title="Disminuir">−</button>
        <span class="qty-num">${item.cantidad}</span>
        <button type="button" class="qty-btn" onclick="changeQty(${i}, 1)" title="Aumentar">+</button>
        <button type="button" class="cart-item-remove" onclick="removeItem(${i})" title="Quitar prenda">×</button>
      </div>
    </div>
  `).join('');

  updateSummary();
}

function getTotal() {
  return state.carrito.reduce((acc, i) => acc + (i.precio * i.cantidad), 0);
}

function updateSummary() {
  const sub = getTotal();
  const desc = 0;
  const iva = 0;
  const total = sub - desc + iva;

  const subEl = document.getElementById('sum-subtotal');
  const descEl = document.getElementById('sum-descuentos');
  const ivaEl = document.getElementById('sum-iva');
  const totalEl = document.getElementById('sum-total');
  const btn = document.getElementById('process-btn');

  if (subEl) subEl.textContent = fmt(sub);
  if (descEl) descEl.textContent = fmt(desc);
  if (ivaEl) ivaEl.textContent = fmt(iva);
  if (totalEl) totalEl.textContent = fmt(total);

  actualizarVueltas(total);

  if (btn) btn.disabled = !validarPagoPOS(total);
}

function obtenerDatosPago() {
  const efectivo = Number(document.getElementById('efectivo-recibido')?.value || 0);
  return {
    efectivoRecibido: efectivo,
    cambioEntregado: Math.max(0, efectivo - getTotal()),
    banco: document.getElementById('transfer-banco')?.value || '',
    referencia: document.getElementById('transfer-referencia')?.value?.trim() || '',
    aprobacion: document.getElementById('credit-aprobacion')?.value?.trim() || ''
  };
}

function validarPagoPOS(total) {
  if (state.carrito.length === 0 || !state.metodoPagoSeleccionado) return false;
  const pago = obtenerDatosPago();
  const flujo = state.metodoPagoSeleccionado.flujo;

  if (flujo === 'efectivo') return pago.efectivoRecibido >= total;
  if (flujo === 'transferencia') return Boolean(pago.banco && pago.referencia);
  if (flujo === 'credito') return Boolean(pago.aprobacion);
  return false;
}

function actualizarVueltas(total) {
  const input = document.getElementById('efectivo-recibido');
  const cambio = document.getElementById('payment-change');
  const fila = document.getElementById('payment-change-row');
  if (!input || !cambio) return;

  const recibido = Number(input.value || 0);
  const vueltas = recibido >= total ? recibido - total : 0;
  cambio.textContent = fmt(vueltas);
  fila?.classList.toggle('is-invalid', recibido > 0 && recibido < total);
}

function changeQty(idx, delta) {
  const item = state.carrito[idx];
  const nuevaCant = item.cantidad + delta;
  if (nuevaCant > item.stockMax) {
    toast(`Stock máximo disponible para esta prenda: ${item.stockMax}`, 'error');
    return;
  }
  if (nuevaCant < 1) {
    removeItem(idx);
    return;
  }
  item.cantidad = nuevaCant;
  renderCarrito();
}

function removeItem(idx) {
  state.carrito.splice(idx, 1);
  renderCarrito();
  toast('Prenda retirada del carrito');
}

// ── Control del Modal Desplegable del Carrito ────────────────────────────────
function abrirModalCarrito() {
  renderCarrito();
  const modal = document.getElementById('modal-carrito');
  if (modal) {
    modal.classList.remove('hidden');
  }
}

function cerrarModalCarrito() {
  const modal = document.getElementById('modal-carrito');
  if (modal) {
    modal.classList.add('hidden');
  }
}

// ── Íconos Dinámicos por Tipo de Prenda ──────────────────────────────────────
function getIconoPrenda(categoria, nombre) {
  const str = ((categoria || '') + ' ' + (nombre || '')).toLowerCase();
  if (str.includes('calzado') || str.includes('zapato') || str.includes('tenis') || str.includes('mocasín')) return '👟';
  if (str.includes('pantalon') || str.includes('jean') || str.includes('jogger') || str.includes('bermuda')) return '👖';
  if (str.includes('polo') || str.includes('camiseta') || str.includes('t-shirt')) return '👕';
  if (str.includes('chaqueta') || str.includes('blazer') || str.includes('abrigo') || str.includes('saco')) return '🧥';
  if (str.includes('accesorio') || str.includes('gorra') || str.includes('cinturón') || str.includes('billetera')) return '🎒';
  return '👔';
}

// ── Búsqueda Dinámica y Filtro en Tiempo Real en el Catálogo ──────────────────
function initSearch() {
  const input = document.getElementById('search-producto');
  if (!input) return;

  // Filtrado instantáneo en vivo al teclear (sin dropdown superpuesto)
  input.addEventListener('input', () => {
    onFiltroPOSCambiado();
  });

  input.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      const q = input.value.trim().toLowerCase();
      if (!q) return;

      // 1. Coincidencia exacta por SKU o Código de Barras
      const exacto = state.catalogoVariantes.find(v =>
        (v.sku && v.sku.toLowerCase() === q) ||
        (v.codigoBarras && v.codigoBarras.toLowerCase() === q)
      );

      if (exacto) {
        agregarVarianteDirecta(exacto.id);
        input.value = '';
        onFiltroPOSCambiado();
        return;
      }

      // 2. Primera coincidencia visible con stock disponible
      const primeroConStock = state.catalogoVariantes.find(v =>
        (v.nombreProducto.toLowerCase().includes(q) || (v.marca && v.marca.toLowerCase().includes(q))) &&
        v.stockActual > 0
      );

      if (primeroConStock) {
        agregarVarianteDirecta(primeroConStock.id);
        input.value = '';
        onFiltroPOSCambiado();
      } else {
        toast('No se encontró ninguna prenda disponible con ese criterio', 'error');
      }
    }
  });
}

function seleccionarChipCategoria(btn, cat) {
  state.categoriaActivaPOS = (cat || '').toLowerCase();
  document.querySelectorAll('.chip-btn').forEach(b => b.classList.remove('active'));
  if (btn) btn.classList.add('active');
  onFiltroPOSCambiado();
}

function onFiltroPOSCambiado() {
  const input = document.getElementById('search-producto');
  const q = input?.value?.trim()?.toLowerCase() || '';
  const clearBtn = document.getElementById('btn-clear-search');

  if (clearBtn) {
    clearBtn.style.display = q ? 'flex' : 'none';
  }

  const variantes = state.catalogoVariantes || [];
  const cat = state.categoriaActivaPOS || '';

  const filtrados = variantes.filter(v => {
    if (q) {
      const matchSku = v.sku && v.sku.toLowerCase().includes(q);
      const matchBarras = v.codigoBarras && v.codigoBarras.toLowerCase().includes(q);
      const matchNombre = v.nombreProducto && v.nombreProducto.toLowerCase().includes(q);
      const matchMarca = v.marca && v.marca.toLowerCase().includes(q);
      const matchColor = v.color && v.color.toLowerCase().includes(q);
      const matchTalla = v.talla && v.talla.toLowerCase().includes(q);
      const matchCat = v.categoria && v.categoria.toLowerCase().includes(q);
      if (!matchSku && !matchBarras && !matchNombre && !matchMarca && !matchColor && !matchTalla && !matchCat) {
        return false;
      }
    }

    if (cat) {
      const vCat = (v.categoria || '').toLowerCase();
      const vNom = (v.nombreProducto || '').toLowerCase();
      
      let matchCat = false;
      if (cat === 'camisas') {
        matchCat = vCat.includes('camisa') || vCat.includes('polo') || vCat.includes('top') ||
                   vNom.includes('camisa') || vNom.includes('polo') || vNom.includes('t-shirt') || vNom.includes('camiseta');
      } else if (cat === 'pantalones') {
        matchCat = (vCat.includes('pantalon') || vCat.includes('jean') || vCat.includes('chino') ||
                   vNom.includes('pantalón') || vNom.includes('pantalon') || vNom.includes('chino') || vNom.includes('jean') || vNom.includes('dress')) &&
                   !vNom.includes('bermuda') && !vCat.includes('bermuda');
      } else if (cat === 'bermudas') {
        matchCat = vCat.includes('bermuda') || vCat.includes('short') || vNom.includes('bermuda') || vNom.includes('short');
      } else if (cat === 'calzado') {
        matchCat = vCat.includes('calzado') || vCat.includes('zapato') || vCat.includes('tenis') ||
                   vNom.includes('zapato') || vNom.includes('tenis') || vNom.includes('mocasín') || vNom.includes('mocasin');
      } else if (cat === 'accesorios') {
        matchCat = vCat.includes('accesorio') || vCat.includes('cinturón') || vCat.includes('cinturon') ||
                   vNom.includes('cinturón') || vNom.includes('cinturon') || vNom.includes('billetera') || vNom.includes('gorra');
      } else {
        matchCat = vCat.includes(cat) || vNom.includes(cat);
      }

      if (!matchCat) {
        return false;
      }
    }

    return true;
  });

  renderCatalogoPOS(filtrados);
}

function limpiarBusquedaPOS() {
  const input = document.getElementById('search-producto');
  if (input) {
    input.value = '';
    input.focus();
  }
  onFiltroPOSCambiado();
}

function renderCatalogoPOS(items = null) {
  const grid = document.getElementById('pos-catalog-grid');
  const countEl = document.getElementById('pos-catalog-count');
  const lista = items !== null ? items : (state.catalogoVariantes || []);

  if (countEl) {
    countEl.textContent = `${lista.length} prendas disponibles`;
  }

  if (!grid) return;

  if (lista.length === 0) {
    grid.innerHTML = `
      <div class="catalog-empty">
        <span class="catalog-empty-icon">🔍</span>
        <div style="font-size:15px; font-weight:700; color:#fff;">No se encontraron prendas</div>
        <div style="font-size:12px; color:var(--text-muted);">Prueba escribiendo otra prenda o cambiando el filtro de categoría</div>
      </div>
    `;
    return;
  }

  grid.innerHTML = lista.map(v => {
    const icon = getIconoPrenda(v.categoria, v.nombreProducto);
    let stockClass = 'in-stock';
    let stockText = `${v.stockActual} en stock`;
    if (v.stockActual <= 0) {
      stockClass = 'out-stock';
      stockText = 'Agotado';
    } else if (v.stockActual <= 5) {
      stockClass = 'low-stock';
      stockText = `Últimas ${v.stockActual}`;
    }

    // Obtener todas las variantes hermanas del mismo producto o nombre para mostrar sus tallas disponibles
    const targetProdId = v.productoId;
    const nombreBase = (v.nombreProducto || '').toLowerCase();
    const hermanas = (state.catalogoVariantes || []).filter(h =>
      (targetProdId && h.productoId === targetProdId) ||
      (h.nombreProducto && h.nombreProducto.toLowerCase() === nombreBase)
    );

    // Tallas únicas ordenadas
    const tallasMap = new Map();
    hermanas.forEach(h => {
      if (!tallasMap.has(h.talla)) {
        tallasMap.set(h.talla, h);
      } else {
        // Preferir la que tenga stock > 0
        if (h.stockActual > 0 && tallasMap.get(h.talla).stockActual <= 0) {
          tallasMap.set(h.talla, h);
        }
      }
    });

    const tallasHtml = Array.from(tallasMap.entries()).map(([talla, varItem]) => {
      const isSelected = varItem.id === v.id;
      const hayStock = varItem.stockActual > 0;
      const cls = [
        'card-size-chip',
        isSelected ? 'selected' : '',
        !hayStock ? 'disabled' : ''
      ].filter(Boolean).join(' ');

      return `
        <button type="button" class="${cls}" 
                onclick="event.stopPropagation(); cambiarVarianteEnCard(${varItem.id})" 
                title="${talla} (${hayStock ? varItem.stockActual + ' disponibles' : 'Agotado'})">
          ${talla}
        </button>
      `;
    }).join('');

    const mediaHtml = v.imagenUrl ? `
      <div class="card-media-box">
        <span class="card-brand-badge">${v.marca || 'Aisaac'}</span>
        <span class="card-stock-badge-floating pill-stock ${stockClass}">${stockText}</span>
        <img src="${v.imagenUrl}" alt="${v.nombreProducto}" class="card-product-img" loading="lazy" onerror="this.onerror=null; this.parentElement.innerHTML='<div class=\\'card-fallback-icon\\'><span class=\\'fashion-icon\\'>${icon}</span><span class=\\'fashion-category\\'>${v.categoria}</span></div>';" />
      </div>
    ` : `
      <div class="card-media-box">
        <span class="card-brand-badge">${v.marca || 'Aisaac'}</span>
        <span class="card-stock-badge-floating pill-stock ${stockClass}">${stockText}</span>
        <div class="card-fallback-icon">
          <span class="fashion-icon">${icon}</span>
          <span class="fashion-category">${v.categoria}</span>
        </div>
      </div>
    `;

    return `
      <div class="catalog-card" role="button" tabindex="0"
           onclick="abrirModalSelectorVariante(${v.productoId || 'null'}, ${v.id})"
           onkeydown="if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); abrirModalSelectorVariante(${v.productoId || 'null'}, ${v.id}); }">
        ${mediaHtml}

        <div class="card-sizes-bar">
          <span style="font-size:10px; color:var(--text-muted); font-weight:700; text-transform:uppercase; margin-right:4px;">Tallas:</span>
          ${tallasHtml}
        </div>

        <div class="card-body">
          <div class="card-title-row">
            <div class="card-title" title="${v.nombreProducto}">${v.nombreProducto}</div>
            <div class="card-sku-color">
              <span class="card-sku-text">${v.sku}</span>
              <span>·</span>
              <span>${v.color}</span>
              <span>·</span>
              <span>Talla <strong>${v.talla}</strong></span>
            </div>
          </div>

          <div class="card-footer">
            <div class="card-price">${fmt(v.precioVenta)}</div>
            <div style="display:flex; gap:6px;">
              <button type="button" class="card-btn-add" ${v.stockActual <= 0 ? 'disabled' : ''}
                      onclick="event.stopPropagation(); agregarVarianteDirecta(${v.id})" title="Agregar al carrito">
                + Agregar
              </button>
              <button type="button" class="btn-pos-variants" onclick="event.stopPropagation(); abrirModalSelectorVariante(${v.productoId || 'null'}, ${v.id})" title="Ver detalles y selector completo">
                Tallas
              </button>
            </div>
          </div>
        </div>
      </div>
    `;
  }).join('');
}

function cambiarVarianteEnCard(varianteId) {
  // Re-render con la variante elegida en foco o simplemente abrir selector modal
  abrirModalSelectorVariante(null, varianteId);
}

function agregarVarianteDirecta(varianteId) {
  const found = state.catalogoVariantes.find(v => v.id === varianteId);
  if (!found) return;

  if (found.stockActual <= 0) {
    toast(`¡Prenda agotada en inventario! (${found.sku})`, 'error');
    return;
  }

  const existing = state.carrito.find(i => i.varianteId === found.id);
  if (existing) {
    if (existing.cantidad >= found.stockActual) {
      toast(`No puedes agregar más: Stock máximo alcanzado (${found.stockActual})`, 'error');
      return;
    }
    existing.cantidad++;
    toast(`+1 ${found.nombreProducto} (${found.talla}/${found.color})`);
  } else {
    state.carrito.push({
      varianteId: found.id,
      sku: found.sku,
      producto: found.nombreProducto,
      variante: `${found.talla} / ${found.color} (${found.marca})`,
      precio: found.precioVenta,
      cantidad: 1,
      stockMax: found.stockActual
    });
    toast(`${found.nombreProducto} agregada al carrito`);
  }
  renderCarrito();
}

// ── Modal Selector Rápido de Tallas y Colores ─────────────────────────────────
let modalVarState = {
  productoId: null,
  nombreProducto: '',
  variantesProducto: [],
  tallaSeleccionada: null,
  colorSeleccionada: null,
  varianteSeleccionada: null,
  cantidad: 1
};

function abrirModalSelectorVariante(productoId, varianteIdInicial = null) {
  let inicial = state.catalogoVariantes.find(v => v.id === varianteIdInicial);
  if (!inicial && state.catalogoVariantes.length > 0) inicial = state.catalogoVariantes[0];
  if (!inicial) return;

  const targetProdId = productoId || inicial.productoId;
  const nombreBase = inicial.nombreProducto;

  const variantesRelacionadas = state.catalogoVariantes.filter(v =>
    (targetProdId && v.productoId === targetProdId) ||
    (v.nombreProducto.toLowerCase() === nombreBase.toLowerCase())
  );

  modalVarState = {
    productoId: targetProdId,
    nombreProducto: inicial.nombreProducto,
    variantesProducto: variantesRelacionadas,
    tallaSeleccionada: inicial.talla,
    colorSeleccionada: inicial.color,
    varianteSeleccionada: inicial,
    cantidad: 1
  };

  document.getElementById('modal-var-title').textContent = inicial.nombreProducto;
  document.getElementById('modal-var-subtitle').textContent = `Selecciona la talla y color para agregar a la orden POS`;
  document.getElementById('modal-var-name').textContent = inicial.nombreProducto;
  document.getElementById('modal-var-category').textContent = `${inicial.categoria || 'Moda'} · ${inicial.marca}`;
  
  const imgBox = document.getElementById('modal-var-img-container');
  if (imgBox) {
    if (inicial.imagenUrl) {
      imgBox.innerHTML = `<img src="${inicial.imagenUrl}" alt="${inicial.nombreProducto}" style="width:100%; height:100%; object-fit:cover; border-radius:8px;" onerror="this.onerror=null; this.parentElement.innerHTML='<span style=\\'font-size:28px\\'>${getIconoPrenda(inicial.categoria, inicial.nombreProducto)}</span>';" />`;
    } else {
      imgBox.innerHTML = `<span style="font-size:28px">${getIconoPrenda(inicial.categoria, inicial.nombreProducto)}</span>`;
    }
  }

  document.getElementById('modal-var-qty').value = 1;

  renderPillsTallasModal();
  renderPillsColoresModal();
  actualizarDetalleVarianteModal();

  document.getElementById('modal-selector-variante').classList.remove('hidden');
}

function renderPillsTallasModal() {
  const container = document.getElementById('modal-var-tallas-container');
  const labelTalla = document.getElementById('modal-selected-size-label');
  if (!container) return;

  if (labelTalla) {
    labelTalla.textContent = modalVarState.tallaSeleccionada ? `Seleccionada: ${modalVarState.tallaSeleccionada}` : '';
  }

  const tallas = [...new Set(modalVarState.variantesProducto.map(v => v.talla))];
  container.innerHTML = tallas.map(t => {
    const active = modalVarState.tallaSeleccionada === t ? 'active' : '';
    const conEsaTalla = modalVarState.variantesProducto.filter(v => v.talla === t);
    const hayStock = conEsaTalla.some(v => v.stockActual > 0);
    const stockClass = !hayStock ? 'disabled' : '';

    return `
      <button type="button" class="fashion-size-btn ${active} ${stockClass}" ${!hayStock ? 'disabled' : ''}
              onclick="seleccionarTallaModal('${t.replace(/'/g, "\\'")}')" title="${t} ${!hayStock ? '(Agotado)' : ''}">
        ${t}
      </button>
    `;
  }).join('');
}

function renderPillsColoresModal() {
  const container = document.getElementById('modal-var-colores-container');
  const labelColor = document.getElementById('modal-selected-color-label');
  if (!container) return;

  if (labelColor) {
    labelColor.textContent = modalVarState.colorSeleccionada ? `Seleccionado: ${modalVarState.colorSeleccionada}` : '';
  }

  const colores = [...new Set(modalVarState.variantesProducto.map(v => v.color))];
  container.innerHTML = colores.map(c => {
    const active = modalVarState.colorSeleccionada === c ? 'active' : '';
    const match = modalVarState.variantesProducto.find(v =>
      v.color === c && (!modalVarState.tallaSeleccionada || v.talla === modalVarState.tallaSeleccionada)
    );
    const hayStock = match ? match.stockActual > 0 : false;
    const stockClass = !hayStock ? 'disabled' : '';

    return `
      <button type="button" class="var-choice-pill ${active} ${stockClass}" ${!hayStock ? 'disabled' : ''}
              onclick="seleccionarColorModal('${c.replace(/'/g, "\\'")}')">
        ${c} ${match ? `(${match.stockActual} uds)` : ''}
      </button>
    `;
  }).join('');
}

function seleccionarTallaModal(talla) {
  const variantesConTalla = modalVarState.variantesProducto.filter(v => v.talla === talla);
  if (!variantesConTalla.some(v => v.stockActual > 0)) {
    toast('Esta talla está agotada.', 'error');
    return;
  }
  modalVarState.tallaSeleccionada = talla;
  renderPillsTallasModal();
  renderPillsColoresModal();
  actualizarDetalleVarianteModal();
}

function seleccionarColorModal(color) {
  const varianteConColor = modalVarState.variantesProducto.find(v =>
    v.color === color &&
    (!modalVarState.tallaSeleccionada || v.talla === modalVarState.tallaSeleccionada)
  );
  if (!varianteConColor || varianteConColor.stockActual <= 0) {
    toast('Esta combinación está agotada.', 'error');
    return;
  }
  modalVarState.colorSeleccionada = color;
  renderPillsTallasModal();
  renderPillsColoresModal();
  actualizarDetalleVarianteModal();
}

function actualizarDetalleVarianteModal() {
  const match = modalVarState.variantesProducto.find(v =>
    v.talla === modalVarState.tallaSeleccionada &&
    v.color === modalVarState.colorSeleccionada
  ) || modalVarState.variantesProducto.find(v => v.talla === modalVarState.tallaSeleccionada) || modalVarState.variantesProducto[0];

  modalVarState.varianteSeleccionada = match;

  const skuEl = document.getElementById('modal-var-sku');
  const priceEl = document.getElementById('modal-var-price');
  const badgeEl = document.getElementById('modal-var-stock-badge');
  const addBtn = document.getElementById('btn-modal-add-cart');
  const qtyInput = document.getElementById('modal-var-qty');

  if (match) {
    if (skuEl) skuEl.textContent = `SKU: ${match.sku} · ${match.marca}`;
    if (priceEl) priceEl.textContent = fmt(match.precioVenta);

    if (match.stockActual > 5) {
      badgeEl.innerHTML = `<span style="color:#3dd68c; font-weight:700;">✓ ${match.stockActual} unidades disponibles en inventario</span>`;
      addBtn.disabled = false;
    } else if (match.stockActual > 0) {
      badgeEl.innerHTML = `<span style="color:#ffaa00; font-weight:700;">⚠ Últimas ${match.stockActual} unidades disponibles</span>`;
      addBtn.disabled = false;
    } else {
      badgeEl.innerHTML = `<span style="color:#ff5555; font-weight:700;">✕ Agotado en esta combinación (${match.talla}/${match.color})</span>`;
      addBtn.disabled = true;
    }

    if (qtyInput) {
      qtyInput.max = match.stockActual > 0 ? match.stockActual : 1;
      if (parseInt(qtyInput.value) > match.stockActual && match.stockActual > 0) {
        qtyInput.value = match.stockActual;
      }
    }
  } else {
    badgeEl.innerHTML = `<span style="color:#888;">No disponible en esta combinación</span>`;
    addBtn.disabled = true;
  }
}

function modificarCantidadModalVar(delta) {
  const input = document.getElementById('modal-var-qty');
  if (!input) return;
  let val = (parseInt(input.value) || 1) + delta;
  const max = modalVarState.varianteSeleccionada ? modalVarState.varianteSeleccionada.stockActual : 1;
  val = Math.max(1, Math.min(val, max));
  input.value = val;
}

function validarCantidadModalVar() {
  const input = document.getElementById('modal-var-qty');
  if (!input) return;
  let val = parseInt(input.value) || 1;
  const max = modalVarState.varianteSeleccionada ? modalVarState.varianteSeleccionada.stockActual : 1;
  input.value = Math.max(1, Math.min(val, max));
}

function confirmarAgregarAlCarritoModal() {
  const v = modalVarState.varianteSeleccionada;
  if (!v || v.stockActual <= 0) {
    toast('Esta combinación no tiene stock disponible', 'error');
    return;
  }

  const cant = parseInt(document.getElementById('modal-var-qty')?.value) || 1;
  const existing = state.carrito.find(i => i.varianteId === v.id);

  if (existing) {
    if (existing.cantidad + cant > v.stockActual) {
      toast(`No puedes agregar más: Stock máximo alcanzado (${v.stockActual})`, 'error');
      return;
    }
    existing.cantidad += cant;
    toast(`+${cant} ${v.nombreProducto} (${v.talla}/${v.color})`);
  } else {
    state.carrito.push({
      varianteId: v.id,
      sku: v.sku,
      producto: v.nombreProducto,
      variante: `${v.talla} / ${v.color} (${v.marca})`,
      precio: v.precioVenta,
      cantidad: cant,
      stockMax: v.stockActual
    });
    toast(`${cant}x ${v.nombreProducto} agregada(s) a la orden`);
  }

  renderCarrito();
  cerrarModal('modal-selector-variante');
}

// Selección de Métodos de Pago
function initPayMethods() {
  document.querySelectorAll('.pay-method-card').forEach(card => {
    card.addEventListener('click', () => {
      document.querySelectorAll('.pay-method-card').forEach(c => {
        c.classList.remove('selected-efectivo', 'selected-transferencia', 'selected-credito');
      });
      const flujo = card.dataset.flujo.toLowerCase();
      card.classList.add(`selected-${flujo}`);
      state.metodoPagoSeleccionado = {
        id: parseInt(card.dataset.method),
        flujo,
        nombre: card.querySelector('.pay-name').textContent,
        proveedor: card.dataset.proveedor || null
      };

      document.getElementById('payment-cash-fields').hidden = flujo !== 'efectivo';
      document.getElementById('payment-transfer-fields').hidden = flujo !== 'transferencia';
      document.getElementById('payment-credit-fields').hidden = flujo !== 'credito';
      updateSummary();
    });
  });

  ['efectivo-recibido', 'transfer-banco', 'transfer-referencia', 'credit-aprobacion'].forEach(id => {
    document.getElementById(id)?.addEventListener('input', () => updateSummary());
  });
  document.getElementById('transfer-banco')?.addEventListener('change', () => updateSummary());
}

// Procesar Venta contra el Backend
async function ejecutarVentaBackend() {
  if (state.carrito.length === 0 || !state.metodoPagoSeleccionado) return;

  if (state.metodoPagoSeleccionado.flujo === 'efectivo' && state.cierreData && state.cierreData.estadoCaja !== 'Abierta') {
    toast('La caja se encuentra cerrada. Por favor abre la caja antes de registrar cobros en efectivo.', 'warning');
    abrirModalAperturaCaja();
    return;
  }

  const btn = document.getElementById('process-btn');
  btn.disabled = true;
  btn.textContent = 'PROCESANDO EN POSTGRESQL...';

  const datosPago = obtenerDatosPago();
  const payload = {
    medioPagoId: state.metodoPagoSeleccionado.id,
    referenciaPago: state.metodoPagoSeleccionado.flujo === 'transferencia' ? datosPago.referencia : null,
    banco: state.metodoPagoSeleccionado.flujo === 'transferencia' ? datosPago.banco : null,
    codigoAprobacion: state.metodoPagoSeleccionado.flujo === 'credito' ? datosPago.aprobacion : null,
    efectivoRecibido: state.metodoPagoSeleccionado.flujo === 'efectivo' ? datosPago.efectivoRecibido : 0,
    cambioEntregado: state.metodoPagoSeleccionado.flujo === 'efectivo' ? datosPago.cambioEntregado : 0,
    descuento: 0,
    items: state.carrito.map(i => ({
      varianteProductoId: i.varianteId,
      cantidad: i.cantidad
    }))
  };

  try {
    const res = await fetchWithAuth('/api/ventas/procesar', {
      method: 'POST',
      body: payload
    });
    const data = await res.json();

    if (!res.ok) {
      toast(data.mensaje || 'Error procesando la venta', 'error');
      btn.disabled = false;
      btn.textContent = 'PROCESAR VENTA Y GENERAR TICKET';
      return;
    }

    toast(`¡Venta #${data.numeroFactura} registrada exitosamente!`);

    // Cerrar modal de carrito
    cerrarModalCarrito();

    // Mostrar Ticket de venta generado
    mostrarTicketModal(data, state.carrito, state.metodoPagoSeleccionado.nombre);

    // Limpiar carrito
    state.carrito = [];
    state.metodoPagoSeleccionado = null;
    document.querySelectorAll('.pay-method-card').forEach(c =>
      c.classList.remove('selected-efectivo', 'selected-transferencia', 'selected-credito'));
    renderCarrito();

    // Estos endpoints están habilitados para Administrador y Cajero; no se recarga
    // ningún módulo administrativo desde el flujo de cobranza de un cajero.
    cargarCatalogoVariantes();
    renderCierre();
  } catch (err) {
    console.error(err);
    toast('Error comunicando con la base de datos', 'error');
  } finally {
    btn.disabled = false;
    btn.innerHTML = `
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
        <polyline points="22 4 12 14.01 9 11.01"/>
      </svg>
      PROCESAR VENTA Y GENERAR TICKET
    `;
  }
}
function mostrarTicketModal(ventaData, items, nombrePago) {
  const container = document.getElementById('ticket-print-area');
  const user = state.usuario ? state.usuario.nombre : 'Cajero Principal';

  container.innerHTML = `
    <div class="ticket-header">
      <div class="ticket-title">AISAAC STR</div>
      <div>SISTEMA DE GESTIÓN RETAIL</div>
      <div>NIT: 900.876.543-2</div>
      <div>CAMPOALEGRE - HUILA</div>
      <div style="margin-top:6px;">FACTURA DE VENTA: <strong>${ventaData.numeroFactura}</strong></div>
      <div>Fecha: ${fmtDateTime(ventaData.fecha)}</div>
      <div>Cajero: ${user}</div>
    </div>

    <div style="margin: 10px 0; border-bottom: 1px dashed #333; padding-bottom: 8px;">
      ${items.map(it => `
        <div style="display:flex; justify-content:space-between; margin-bottom:4px;">
          <span>${it.cantidad}x ${it.producto} (${it.sku})</span>
          <span>${fmt(it.precio * it.cantidad)}</span>
        </div>
      `).join('')}
    </div>

    <div style="display:flex; justify-content:space-between; font-weight:bold; font-size:14px;">
      <span>TOTAL PAGADO:</span>
      <span>${fmt(ventaData.total)}</span>
    </div>
    <div style="display:flex; justify-content:space-between; margin-top:4px;">
      <span>Método de Pago:</span>
      <span>${nombrePago}</span>
    </div>

    <div class="ticket-footer">
      <div>¡Gracias por su compra en AISAAC STR!</div>
      <div>Garantía de 30 días por defectos de fábrica</div>
      <div>Software Validado FET - Rivera Huila</div>
    </div>
  `;

  document.getElementById('modal-ticket').classList.remove('hidden');
}

// ═════════════════════════════════════════════════════════════════════════════
// 3. CIERRE Y ARQUEO DE CAJA
// ═════════════════════════════════════════════════════════════════════════════

async function renderCierre() {
  const dateLabel = document.getElementById('cierre-date-label');
  if (dateLabel) dateLabel.textContent = fmtDate();

  try {
    const res = await fetchWithAuth('/api/cierrecaja/hoy');
    if (!res.ok) return;
    const data = await res.json();
    state.cierreData = data;

    const estaAbierta = (data.estadoCaja === 'Abierta');

    // 1. Actualizar botón y estado en el Topbar
    const btnCajaTopbar = document.getElementById('btn-topbar-caja');
    const dotCajaTopbar = document.getElementById('topbar-caja-dot');
    const textCajaTopbar = document.getElementById('topbar-caja-text');
    if (btnCajaTopbar && dotCajaTopbar && textCajaTopbar) {
      if (estaAbierta) {
        btnCajaTopbar.className = 'topbar-caja-btn abierta';
        dotCajaTopbar.className = 'caja-status-dot green';
        textCajaTopbar.innerHTML = `Caja Abierta · <strong style="color:var(--gold-bright);">${fmt(data.montoInicial || 0)}</strong>`;
        btnCajaTopbar.title = `Caja abierta por ${data.responsable || 'Cajero'}. Clic para ver arqueo / cerrar caja`;
      } else {
        btnCajaTopbar.className = 'topbar-caja-btn cerrada';
        dotCajaTopbar.className = 'caja-status-dot red';
        textCajaTopbar.innerHTML = `Caja Cerrada · <strong style="color:var(--gold-bright);">Abrir caja</strong>`;
        btnCajaTopbar.title = `La caja se encuentra cerrada. Clic para Abrir caja`;
      }
    }

    // 2. Actualizar Banner de Estado en panel-cierre
    const banner = document.getElementById('cierre-shift-banner');
    if (banner) {
      if (estaAbierta) {
        banner.className = 'cierre-shift-card abierta';
        const horaApertura = data.fechaApertura ? fmtDateTime(data.fechaApertura) : '--/--/---- --:--';
        banner.innerHTML = `
          <div style="display:flex; flex-direction:column; gap:8px;">
            <div style="display:flex; align-items:center; gap:10px;">
              <span class="shift-status-badge abierta">🟢 Caja Abierta y Activa</span>
              <span style="font-size:12px; color:var(--text-secondary);">Turno operando en tiempo real</span>
            </div>
            <div class="shift-meta-grid">
              <div class="shift-meta-item">
                <span class="shift-meta-label">Responsable</span>
                <span class="shift-meta-val" style="color:var(--gold-bright);">${data.responsable || 'Cajero'}</span>
              </div>
              <div class="shift-meta-item">
                <span class="shift-meta-label">Fecha y Hora Apertura</span>
                <span class="shift-meta-val">${horaApertura}</span>
              </div>
              <div class="shift-meta-item">
                <span class="shift-meta-label">Base Inicial (Fondo)</span>
                <span class="shift-meta-val">${fmt(data.montoInicial || 0)} 🔒</span>
              </div>
              <div class="shift-meta-item">
                <span class="shift-meta-label">Ventas del Turno</span>
                <span class="shift-meta-val">${data.totalVentasCount || 0} tickets</span>
              </div>
            </div>
          </div>
          <div style="display:flex; gap:10px; align-items:center;">
            <button class="btn-cerrar-caja-action" onclick="enfocarArqueoCierre()" title="Ir al arqueo de conteo físico y cerrar caja">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
              </svg>
              Cerrar caja
            </button>
          </div>
        `;
      } else {
        banner.className = 'cierre-shift-card cerrada';
        let ultimoCierreHtml = '';
        if (data.ultimoCierre) {
          const horaCierre = data.ultimoCierre.fechaCierre ? fmtDateTime(data.ultimoCierre.fechaCierre) : 'Hoy';
          ultimoCierreHtml = `
            <div style="font-size:11px; color:var(--text-secondary); margin-top:4px;">
              Último turno: Cerrado el ${horaCierre} por <strong>${data.ultimoCierre.responsable}</strong> · Diferencia: <span style="font-family:'JetBrains Mono'; color:${data.ultimoCierre.diferencia === 0 ? 'var(--green-accent)' : 'var(--gold-bright)'};">${fmt(data.ultimoCierre.diferencia)}</span> (${data.ultimoCierre.estado})
            </div>
          `;
        }
        banner.innerHTML = `
          <div>
            <div style="display:flex; align-items:center; gap:10px;">
              <span class="shift-status-badge cerrada">🔴 Caja Cerrada</span>
              <span style="font-weight:700; font-size:14px; color:var(--text-primary);">No hay un turno de caja activo</span>
            </div>
            <div style="font-size:12px; color:var(--text-muted); margin-top:4px;">
              Para facturar y registrar cobros en efectivo, debes iniciar el turno indicando la base inicial.
            </div>
            ${ultimoCierreHtml}
          </div>
          <button class="btn-abrir-caja-action" onclick="abrirModalAperturaCaja()">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 9.9-1"></path>
            </svg>
            Abrir caja
          </button>
        `;
      }
    }

    // 3. Actualizar fondo inicial / apertura (fijo y bloqueado)
    const kpiBase = document.getElementById('kpi-base-display');
    if (kpiBase) kpiBase.textContent = fmt(data.montoInicial || 0);

    const inputBase = document.getElementById('cierre-monto-inicial');
    if (inputBase) {
      inputBase.value = data.montoInicial || 0;
      inputBase.disabled = true;
    }

    // 4. Actualizar KPIs discriminados
    const kpiEf = document.getElementById('kpi-efectivo');
    if (kpiEf) kpiEf.textContent = fmt(data.totalEfectivo);

    const kpiEg = document.getElementById('kpi-egresos');
    if (kpiEg) kpiEg.textContent = fmt(data.totalPagosProveedores);

    const kpiTrans = document.getElementById('kpi-transferencia');
    if (kpiTrans) kpiTrans.textContent = fmt(data.totalTransferencias);

    const kpiCred = document.getElementById('kpi-credito');
    if (kpiCred) kpiCred.textContent = fmt(data.totalCredito);

    const cierreTotal = document.getElementById('cierre-total');
    if (cierreTotal) cierreTotal.textContent = fmt(data.totalDia);

    // 5. Renderizar lista de pagos/egresos a proveedores en el turno
    renderEgresosCierre(data.egresosDetalle);

    calcDiferenciaCierre();
  } catch (err) {
    console.error('Error cargando cierre:', err);
  }
}

function renderEgresosCierre(egresos) {
  const container = document.getElementById('cierre-egresos-list');
  const countEl = document.getElementById('cierre-egresos-count');
  if (!container) return;
  if (!egresos || egresos.length === 0) {
    if (countEl) countEl.textContent = '0 comprobante(s)';
    container.innerHTML = '<div style="font-size: 11px; color: var(--text-muted); padding: 8px 0;">No se registran egresos de compras en efectivo para este turno.</div>';
    return;
  }
  if (countEl) countEl.textContent = `${egresos.length} comprobante(s)`;
  container.innerHTML = egresos.map(e => `
    <div style="display:flex; justify-content:space-between; align-items:center; padding:6px 0; border-bottom:1px solid rgba(255,255,255,0.05); font-size:12px;">
      <div>
        <strong style="color:var(--text-primary);">${e.proveedor}</strong>
        <span style="color:var(--text-muted); font-size:11px; margin-left:6px;">Factura #${e.numeroFactura || e.id}</span>
      </div>
      <div style="font-family:'JetBrains Mono'; font-weight:700; color:var(--red-accent);">
        - ${fmt(e.total)}
      </div>
    </div>
  `).join('');
}

function calcDiferenciaCierre() {
  if (!state.cierreData) return;
  const base = state.cierreData.montoInicial || 0;
  const ventasEfectivo = state.cierreData.totalEfectivo || 0;
  const egresosProveedores = state.cierreData.totalPagosProveedores || 0;

  // Fórmula contable: Total Esperado = Monto Inicial + Ventas Efectivo - Pagos Proveedores
  const totalEsperado = base + ventasEfectivo - egresosProveedores;

  const esperadoVal = document.getElementById('esperado-val');
  if (esperadoVal) esperadoVal.textContent = fmt(totalEsperado);

  const calcBase = document.getElementById('calc-base');
  if (calcBase) calcBase.textContent = fmt(base);

  const calcVentas = document.getElementById('calc-ventas');
  if (calcVentas) calcVentas.textContent = fmt(ventasEfectivo);

  const calcEgresos = document.getElementById('calc-egresos');
  if (calcEgresos) calcEgresos.textContent = fmt(egresosProveedores);

  const contadoInput = document.getElementById('efectivo-contado');
  const el = document.getElementById('diferencia-val');
  const textEl = document.getElementById('dif-status-text');
  if (!el || !textEl) return;

  if (!contadoInput || contadoInput.value === '') {
    el.textContent = '—';
    el.className = 'dif-val';
    textEl.textContent = 'Ingresa el valor contado físicamente en el cajón para auditar el arqueo';
    return;
  }

  const contado = parseFloat(contadoInput.value) || 0;
  const dif = contado - totalEsperado;

  el.textContent = fmt(Math.abs(dif));
  if (dif === 0) {
    el.className = 'dif-val positivo';
    textEl.innerHTML = '<span style="color:var(--green-accent); font-weight:700;">✓ CAJA CUADRADA EXACTA</span> — El efectivo contado coincide perfectamente con el sistema.';
  } else if (dif > 0) {
    el.className = 'dif-val positivo';
    textEl.innerHTML = `<span style="color:var(--gold-bright); font-weight:700;">⚠️ SOBRANTE DE DINERO (+${fmt(dif)})</span> — Hay más efectivo físico en el cajón que lo registrado.`;
  } else {
    el.className = 'dif-val negativo';
    textEl.innerHTML = `<span style="color:var(--red-accent); font-weight:700;">❌ FALTANTE DE DINERO (-${fmt(Math.abs(dif))})</span> — Falta dinero físico en cajón respecto a las ventas del turno.`;
  }
}

// Modal de Apertura de Caja
function abrirModalAperturaCaja() {
  const nombreEl = document.getElementById('apertura-cajero-nombre');
  if (nombreEl) {
    nombreEl.textContent = state.usuario?.nombre || 'Cajero';
  }
  const fechaHoraEl = document.getElementById('apertura-fecha-hora');
  if (fechaHoraEl) {
    fechaHoraEl.textContent = fmtDateTime(new Date());
  }
  const montoInput = document.getElementById('apertura-monto-inicial');
  if (montoInput && (!montoInput.value || montoInput.value === '0')) {
    montoInput.value = '100000';
  }
  const modal = document.getElementById('modal-abrir-caja');
  if (modal) modal.classList.remove('hidden');
}

async function confirmarAperturaCajaBackend() {
  const montoInput = document.getElementById('apertura-monto-inicial');
  const monto = parseFloat(montoInput?.value);
  if (isNaN(monto) || monto < 0) {
    toast('Ingresa un monto inicial válido mayor o igual a $ 0.', 'error');
    return;
  }

  const payload = {
    usuarioId: state.usuario?.id,
    montoInicial: monto,
    observaciones: document.getElementById('apertura-observaciones')?.value?.trim() || ''
  };

  try {
    const res = await fetchWithAuth('/api/cierrecaja/apertura', {
      method: 'POST',
      body: payload
    });
    const data = await res.json();
    if (!res.ok) {
      toast(data.mensaje || 'Error al abrir caja', 'error');
      return;
    }

    cerrarModal('modal-abrir-caja');
    toast(`¡Caja abierta exitosamente! Responsable: ${data.responsable || state.usuario?.nombre}. Base: ${fmt(data.montoInicial)}`);
    await renderCierre();
  } catch (err) {
    console.error(err);
    toast('Error conectando al servidor para abrir caja', 'error');
  }
}

function manejarClickBotonCajaTopbar() {
  if (state.cierreData && state.cierreData.estadoCaja === 'Abierta') {
    const tabCierre = document.getElementById('tab-cierre');
    if (tabCierre) tabCierre.click();
    enfocarArqueoCierre();
  } else {
    abrirModalAperturaCaja();
  }
}

function enfocarArqueoCierre() {
  const contadoInput = document.getElementById('efectivo-contado');
  if (contadoInput) {
    contadoInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
    contadoInput.focus();
  }
}

// Cerrar Caja y Confirmar Arqueo Final
async function ejecutarCierreBackend() {
  if (!state.cierreData || state.cierreData.estadoCaja !== 'Abierta') {
    toast('No hay una caja abierta actualmente para cerrar.', 'error');
    return;
  }

  const contadoInput = document.getElementById('efectivo-contado');
  const contado = parseFloat(contadoInput?.value);
  if (isNaN(contado) || contado < 0) {
    toast('Debes ingresar el valor de efectivo contado físicamente en el cajón.', 'error');
    enfocarArqueoCierre();
    return;
  }

  const base = state.cierreData.montoInicial || 0;
  const payload = {
    usuarioId: state.usuario?.id,
    montoInicial: base,
    efectivoContadoFisico: contado,
    observaciones: document.getElementById('cierre-observaciones')?.value?.trim() || ''
  };

  try {
    const res = await fetchWithAuth('/api/cierrecaja/confirmar', {
      method: 'POST',
      body: payload
    });
    const data = await res.json();
    if (!res.ok) {
      toast(data.mensaje || 'Error al guardar cierre', 'error');
      return;
    }

    toast(`¡Cierre de caja registrado exitosamente! Conciliación: ${data.estadoDiferencia}`);

    // Mostrar modal con el comprobante completo auditado
    mostrarComprobanteCierre(data);

    if (contadoInput) contadoInput.value = '';
    const obsInput = document.getElementById('cierre-observaciones');
    if (obsInput) obsInput.value = '';

    await renderCierre();
  } catch (err) {
    console.error(err);
    toast('Error registrando cierre en la base de datos', 'error');
  }
}

function mostrarComprobanteCierre(data) {
  const ticketArea = document.getElementById('cierre-resumen-ticket');
  if (!ticketArea) return;

  const difColor = data.diferencia === 0 ? 'var(--green-accent)' : (data.diferencia > 0 ? 'var(--gold-bright)' : 'var(--red-accent)');
  const difSimbolo = data.diferencia > 0 ? '+' : '';

  ticketArea.innerHTML = `
    <div style="text-align:center; border-bottom:1px dashed var(--border-gold); padding-bottom:12px; margin-bottom:12px;">
      <div style="font-size:16px; font-weight:800; color:var(--gold-bright); letter-spacing:1px;">AISAAC STR</div>
      <div style="font-size:11px; color:var(--text-secondary);">RETAIL & FASHION ERP</div>
      <div style="font-size:13px; font-weight:700; color:var(--text-primary); margin-top:6px;">COMPROBANTE DE ARQUEO Y CIERRE DE CAJA</div>
      <div style="font-size:11px; color:var(--text-muted); font-family:'JetBrains Mono'; margin-top:2px;">Cierre #${data.cierreId} · ${fmtDateTime(data.fechaCierre || new Date())}</div>
    </div>

    <div style="font-size:12px; display:flex; flex-direction:column; gap:6px; margin-bottom:14px;">
      <div style="display:flex; justify-content:space-between;">
        <span style="color:var(--text-muted);">Responsable:</span>
        <strong style="color:var(--text-primary);">${data.responsable || state.usuario?.nombre || 'Cajero'}</strong>
      </div>
      <div style="display:flex; justify-content:space-between;">
        <span style="color:var(--text-muted);">Fecha y Hora Apertura:</span>
        <span style="font-family:'JetBrains Mono';">${fmtDateTime(data.fechaApertura)}</span>
      </div>
      <div style="display:flex; justify-content:space-between;">
        <span style="color:var(--text-muted);">Fecha y Hora Cierre:</span>
        <span style="font-family:'JetBrains Mono';">${fmtDateTime(data.fechaCierre || new Date())}</span>
      </div>
    </div>

    <div style="border-top:1px dashed rgba(255,255,255,0.1); border-bottom:1px dashed rgba(255,255,255,0.1); padding:10px 0; margin-bottom:14px; font-size:12px; display:flex; flex-direction:column; gap:6px;">
      <div style="display:flex; justify-content:space-between;">
        <span>(+) Base Inicial (Apertura)</span>
        <span style="font-family:'JetBrains Mono';">${fmt(data.montoInicial)}</span>
      </div>
      <div style="display:flex; justify-content:space-between;">
        <span>(+) Ventas Efectivo</span>
        <span style="font-family:'JetBrains Mono'; color:var(--green-accent);">${fmt(data.totalVentasEfectivo)}</span>
      </div>
      <div style="display:flex; justify-content:space-between;">
        <span>(-) Egresos Compras Proveedor</span>
        <span style="font-family:'JetBrains Mono'; color:var(--red-accent);">- ${fmt(data.totalPagosProveedores || 0)}</span>
      </div>
      <div style="display:flex; justify-content:space-between;">
        <span style="color:var(--text-secondary);">Ventas Bancos (Nequi/PSE)</span>
        <span style="font-family:'JetBrains Mono';">${fmt(data.totalTransferencias || 0)}</span>
      </div>
      <div style="display:flex; justify-content:space-between;">
        <span style="color:var(--text-secondary);">Ventas Crédito (Sistecrédito/Addi)</span>
        <span style="font-family:'JetBrains Mono';">${fmt(data.totalCredito || 0)}</span>
      </div>
    </div>

    <div style="background:rgba(212,168,67,0.06); border:1px solid rgba(212,168,67,0.25); border-radius:8px; padding:12px; margin-bottom:12px; font-size:13px; display:flex; flex-direction:column; gap:8px;">
      <div style="display:flex; justify-content:space-between; align-items:center;">
        <span style="font-weight:600; color:var(--text-secondary);">Total Efectivo Esperado:</span>
        <strong style="font-family:'JetBrains Mono'; font-size:15px; color:var(--gold-bright);">${fmt(data.totalEsperado)}</strong>
      </div>
      <div style="display:flex; justify-content:space-between; align-items:center;">
        <span style="font-weight:600; color:var(--text-secondary);">Efectivo Contado en Cajón:</span>
        <strong style="font-family:'JetBrains Mono'; font-size:15px; color:var(--text-primary);">${fmt(data.totalReal)}</strong>
      </div>
      <div style="display:flex; justify-content:space-between; align-items:center; border-top:1px solid rgba(255,255,255,0.1); padding-top:6px;">
        <span style="font-weight:700;">Resultado Conciliación:</span>
        <strong style="font-family:'JetBrains Mono'; font-size:16px; color:${difColor};">${difSimbolo}${fmt(data.diferencia)} (${data.estadoDiferencia})</strong>
      </div>
    </div>

    <div style="text-align:center; font-size:11px; color:var(--text-muted); margin-top:8px;">
      Auditoría de arqueo de caja conforme a política interna.
    </div>
  `;

  const modal = document.getElementById('modal-resumen-cierre');
  if (modal) modal.classList.remove('hidden');
}

function imprimirComprobanteCierre() {
  window.print();
}

// ═════════════════════════════════════════════════════════════════════════════
// 4. MÓDULO DE INVENTARIO Y MATRIZ DE VARIANTES
// ═════════════════════════════════════════════════════════════════════════════

let inventarioEstado = {
  pagina: 1,
  totalPaginas: 1,
  totalRegistros: 0,
  limite: 20,
  busqueda: ''
};

let debounceTimerInventario = null;

async function cargarInventario(pagina = inventarioEstado.pagina) {
  try {
    inventarioEstado.pagina = pagina || 1;
    const searchInput = document.getElementById('inv-search');
    const q = searchInput ? searchInput.value.trim() : (inventarioEstado.busqueda || '');
    inventarioEstado.busqueda = q;

    const params = new URLSearchParams();
    params.set('pagina', inventarioEstado.pagina.toString());
    if (q) params.set('busqueda', q);

    const res = await fetchWithAuth(`/api/inventario/productos?${params.toString()}`);
    if (!res.ok) return;
    const data = await res.json();

    const productos = Array.isArray(data) ? data : (data.productos || []);
    if (!Array.isArray(data)) {
      inventarioEstado.pagina = data.pagina || inventarioEstado.pagina;
      inventarioEstado.totalPaginas = data.totalPaginas || 1;
      inventarioEstado.totalRegistros = data.totalRegistros || 0;
      inventarioEstado.limite = data.limite || 20;
    }

    renderTablaInventario(productos);
    actualizarControlesPaginacionInventario();
  } catch (err) {
    console.error('Error cargando inventario:', err);
  }
}

function cambiarPaginaInventario(pagina) {
  if (pagina < 1 || pagina > inventarioEstado.totalPaginas || pagina === inventarioEstado.pagina) return;
  cargarInventario(pagina);
  const scrollContainer = document.querySelector('.inventory-table-scroll');
  if (scrollContainer) scrollContainer.scrollTop = 0;
}

function actualizarControlesPaginacionInventario() {
  const { pagina, totalPaginas, totalRegistros } = inventarioEstado;

  // Actualizar texto informativo
  const infoEl = document.getElementById('inv-paginacion-info');
  if (infoEl) {
    if (totalRegistros === 0) {
      infoEl.innerHTML = 'No se encontraron prendas';
    } else {
      const inicio = (pagina - 1) * inventarioEstado.limite + 1;
      const fin = Math.min(pagina * inventarioEstado.limite, totalRegistros);
      infoEl.innerHTML = `Mostrando <span>${inicio}–${fin}</span> de <span>${totalRegistros}</span> productos (Pág. <span>${pagina}</span> de <span>${totalPaginas}</span>)`;
    }
  }

  // Actualizar botón Anterior
  const btnPrev = document.getElementById('btn-pag-prev');
  if (btnPrev) {
    btnPrev.disabled = pagina <= 1;
  }

  // Actualizar botón Siguiente
  const btnNext = document.getElementById('btn-pag-next');
  if (btnNext) {
    btnNext.disabled = pagina >= totalPaginas || totalPaginas === 0;
  }

  // Generar botones numéricos
  const numerosContainer = document.getElementById('inv-pag-numeros');
  if (!numerosContainer) return;

  if (totalPaginas <= 1) {
    numerosContainer.innerHTML = totalPaginas === 1 ? `<button type="button" class="btn-page-number active">1</button>` : '';
    return;
  }

  let html = '';
  if (totalPaginas <= 7) {
    for (let i = 1; i <= totalPaginas; i++) {
      html += `<button type="button" class="btn-page-number ${i === pagina ? 'active' : ''}" onclick="cambiarPaginaInventario(${i})">${i}</button>`;
    }
  } else {
    const pages = [];
    pages.push(1);
    if (pagina > 3) pages.push('...');

    const start = Math.max(2, pagina - 1);
    const end = Math.min(totalPaginas - 1, pagina + 1);
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    if (pagina < totalPaginas - 2) pages.push('...');
    pages.push(totalPaginas);

    pages.forEach(p => {
      if (p === '...') {
        html += `<span class="pagination-ellipsis">…</span>`;
      } else {
        html += `<button type="button" class="btn-page-number ${p === pagina ? 'active' : ''}" onclick="cambiarPaginaInventario(${p})">${p}</button>`;
      }
    });
  }

  numerosContainer.innerHTML = html;
}

function renderTablaInventario(productos) {
  const tbody = document.getElementById('inventario-body');
  if (!tbody) return;

  if (!productos || productos.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="12" style="text-align:center; padding:36px 16px; color:var(--text-muted);">
          No se encontraron prendas ni variantes para el filtro seleccionado.
        </td>
      </tr>`;
    return;
  }

  const filas = [];
  productos.forEach(p => {
    p.variantes.forEach(v => {
      let badgeClass = 'normal';
      let badgeLabel = 'En Stock';
      if (v.stockActual === 0) {
        badgeClass = 'agotado';
        badgeLabel = 'AGOTADO';
      } else if (v.stockActual <= v.stockMinimo / 2) {
        badgeClass = 'critico';
        badgeLabel = 'CRÍTICO';
      } else if (v.stockActual <= v.stockMinimo) {
        badgeClass = 'bajo';
        badgeLabel = 'BAJO STOCK';
      }

      const icon = getIconoPrenda(p.categoria, p.nombre);
      const imgHtml = p.imagenUrl 
        ? `<img src="${p.imagenUrl}" alt="${p.nombre}" style="width:36px; height:36px; object-fit:cover; border-radius:6px; border:1px solid rgba(212,168,67,0.3);" onerror="this.parentElement.innerHTML='<span style=\\'font-size:20px\\'>${icon}</span>';" />`
        : `<span style="font-size:22px;">${icon}</span>`;

      filas.push(`
        <tr>
          <td style="text-align:center;">${imgHtml}</td>
          <td><span class="product-code">${v.sku}</span></td>
          <td><strong>${p.nombre}</strong> <span style="font-size:11px; color:var(--text-muted)">(${p.categoria})</span></td>
          <td>${v.talla}</td>
          <td>${v.color}</td>
          <td>${v.marca}</td>
          <td style="font-family:'JetBrains Mono'">${fmt(v.precioCompra)}</td>
          <td style="font-family:'JetBrains Mono'; font-weight:600; color:var(--gold-bright)">${fmt(v.precioVenta)}</td>
          <td><strong style="font-size:15px">${v.stockActual}</strong> un.</td>
          <td style="font-size:11px; color:var(--text-muted)">Mín: ${v.stockMinimo} | Máx: ${v.stockMaximo}</td>
          <td><span class="badge-stock ${badgeClass}">${badgeLabel}</span></td>
          <td>
            <button class="btn-gestionar-inv" onclick="abrirModalGestionProducto(${p.id}, ${v.id})" title="Gestionar prenda, precios, niveles de stock y agregar unidades">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>
              Gestionar
            </button>
          </td>
        </tr>
      `);
    });
  });

  tbody.innerHTML = filas.join('');
}

// ── GESTIÓN Y EDICIÓN DE PRODUCTO / VARIANTES / ENTRADAS ───────────────────────

async function abrirModalGestionProducto(productoId, varianteIdPreseleccionada = null) {
  try {
    const res = await fetchWithAuth(`/api/inventario/producto/${productoId}`);
    if (!res.ok) {
      toast('Error al consultar datos del producto', 'error');
      return;
    }

    const data = await res.json();
    state.productoEnGestion = data;

    // 1. Cabecera del modal
    const nombreHeader = document.getElementById('gp-header-nombre');
    if (nombreHeader) nombreHeader.textContent = data.nombre;

    const catHeader = document.getElementById('gp-header-categoria');
    if (catHeader) catHeader.textContent = data.categoria;

    const codHeader = document.getElementById('gp-header-codigo');
    if (codHeader) codHeader.textContent = `Código: ${data.codigoBase} (${data.variantes.length} variantes)`;

    const imgBox = document.getElementById('gp-header-img-box');
    if (imgBox) {
      const icon = getIconoPrenda(data.categoria, data.nombre);
      imgBox.innerHTML = data.imagenUrl
        ? `<img src="${data.imagenUrl}" alt="${data.nombre}" style="width:100%; height:100%; object-fit:cover;" onerror="this.parentElement.innerHTML='<span style=\\'font-size:26px\\'>${icon}</span>';" />`
        : `<span style="font-size:26px;">${icon}</span>`;
    }

    // 2. Tab 1: Entrada de Stock
    const respInput = document.getElementById('gp-entrada-responsable');
    if (respInput) respInput.value = state.usuario?.nombre || 'Usuario';

    const selectVariantes = document.getElementById('gp-entrada-variante-select');
    if (selectVariantes) {
      selectVariantes.innerHTML = data.variantes.map(v => `
        <option value="${v.id}" data-stock="${v.stockActual}" data-talla="${v.talla}" data-color="${v.color}" data-sku="${v.sku}">
          Talla: ${v.talla} · Color: ${v.color} (SKU: ${v.sku}) — Stock actual: ${v.stockActual} unidades
        </option>
      `).join('');

      if (varianteIdPreseleccionada) {
        selectVariantes.value = varianteIdPreseleccionada;
      }
    }

    actualizarInfoVarianteSeleccionada();
    calcularNuevoStockEntrada();

    // 3. Tab 2: Precios y Umbrales Mín/Máx
    const variantesTbody = document.getElementById('gp-variantes-table-body');
    if (variantesTbody) {
      variantesTbody.innerHTML = data.variantes.map(v => `
        <tr>
          <td><strong style="color:var(--text-primary); font-size:13px;">${v.talla}</strong> / ${v.color} <div style="font-size:10px; color:var(--text-muted);">${v.marca}</div></td>
          <td><span class="product-code" style="font-size:11px;">${v.sku}</span></td>
          <td>
            <input type="number" class="gp-input-cell" id="gp-pcompra-${v.id}" value="${v.precioCompra}" min="0" step="500" />
          </td>
          <td>
            <input type="number" class="gp-input-cell" id="gp-pventa-${v.id}" value="${v.precioVenta}" min="0" step="500" style="color:var(--gold-bright); font-weight:700;" />
          </td>
          <td>
            <input type="number" class="gp-input-cell" id="gp-smin-${v.id}" value="${v.stockMinimo}" min="0" step="1" />
          </td>
          <td>
            <input type="number" class="gp-input-cell" id="gp-smax-${v.id}" value="${v.stockMaximo}" min="1" step="1" />
          </td>
          <td><strong style="font-family:'JetBrains Mono'; font-size:13px;">${v.stockActual}</strong> un.</td>
          <td>
            <button class="gp-btn-save-sm" onclick="guardarVariantePreciosNiveles(${v.id})" title="Guardar precios y niveles para esta variante">
              Guardar
            </button>
          </td>
        </tr>
      `).join('');
    }

    // 4. Tab 3: Datos Generales
    document.getElementById('gp-edit-producto-id').value = data.id;
    document.getElementById('gp-edit-nombre').value = data.nombre;
    document.getElementById('gp-edit-codigo').value = data.codigoBase;
    document.getElementById('gp-edit-categoria').value = data.categoria;
    document.getElementById('gp-edit-imagen').value = '';
    document.getElementById('gp-edit-descripcion').value = data.descripcion || '';

    // 5. Tab 4: Historial de Entradas
    renderHistorialEntradasGP(data.entradasRecientes);

    // Footer info
    const footerInfo = document.getElementById('gp-footer-info');
    if (footerInfo) footerInfo.textContent = `Prenda ID #${data.id} · Stock Total: ${data.stockTotal} unidades en ${data.variantes.length} variantes`;

    // Abrir modal
    document.getElementById('modal-gestionar-producto').classList.remove('hidden');
  } catch (err) {
    console.error(err);
    toast('Error cargando gestión del producto', 'error');
  }
}

function cambiarSubTabGestion(tabId) {
  document.querySelectorAll('.gp-tab-btn').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.gptab === tabId);
  });
  document.querySelectorAll('.gp-tab-content').forEach(content => {
    content.classList.toggle('active', content.id === tabId);
  });
}

function actualizarInfoVarianteSeleccionada() {
  const select = document.getElementById('gp-entrada-variante-select');
  if (!select || !select.selectedOptions || select.selectedOptions.length === 0) return;
  const opt = select.selectedOptions[0];
  const stock = parseInt(opt.dataset.stock) || 0;

  const stockActualEl = document.getElementById('gp-info-stock-actual');
  if (stockActualEl) stockActualEl.textContent = `${stock} un.`;

  calcularNuevoStockEntrada();
}

function calcularNuevoStockEntrada() {
  const select = document.getElementById('gp-entrada-variante-select');
  if (!select || !select.selectedOptions || select.selectedOptions.length === 0) return;
  const opt = select.selectedOptions[0];
  const stockActual = parseInt(opt.dataset.stock) || 0;

  const cantidadInput = document.getElementById('gp-entrada-cantidad');
  const cantidad = parseInt(cantidadInput?.value) || 0;

  const sumarEl = document.getElementById('gp-info-stock-sumar');
  if (sumarEl) sumarEl.textContent = `+${cantidad} un.`;

  const nuevoEl = document.getElementById('gp-info-stock-nuevo');
  if (nuevoEl) nuevoEl.textContent = `${stockActual + cantidad} un.`;
}

async function guardarEntradaStockSubmit(e) {
  if (e) e.preventDefault();
  const select = document.getElementById('gp-entrada-variante-select');
  const varianteId = parseInt(select?.value);
  const cantidadInput = document.getElementById('gp-entrada-cantidad');
  const cantidad = parseInt(cantidadInput?.value);
  const motivo = document.getElementById('gp-entrada-motivo')?.value?.trim();

  if (!varianteId || isNaN(cantidad) || cantidad <= 0) {
    toast('Debes seleccionar una variante e indicar una cantidad mayor a 0.', 'error');
    return;
  }

  const payload = {
    varianteId,
    cantidad,
    usuarioId: state.usuario?.id,
    motivo: motivo || 'Entrada directa de inventario'
  };

  try {
    const res = await fetchWithAuth('/api/inventario/variante/entrada', {
      method: 'POST',
      body: payload
    });
    const data = await res.json();
    if (!res.ok) {
      toast(data.mensaje || 'Error registrando entrada', 'error');
      return;
    }

    toast(data.mensaje);

    // Recargar inventario y modal
    if (state.productoEnGestion) {
      await abrirModalGestionProducto(state.productoEnGestion.id, varianteId);
      cambiarSubTabGestion('gptab-historial');
    }
    cargarInventario();
    cargarCatalogoVariantes();
  } catch (err) {
    console.error(err);
    toast('Error conectando al servidor para registrar entrada', 'error');
  }
}

async function guardarVariantePreciosNiveles(varianteId) {
  const pcompra = parseFloat(document.getElementById(`gp-pcompra-${varianteId}`)?.value);
  const pventa = parseFloat(document.getElementById(`gp-pventa-${varianteId}`)?.value);
  const smin = parseInt(document.getElementById(`gp-smin-${varianteId}`)?.value);
  const smax = parseInt(document.getElementById(`gp-smax-${varianteId}`)?.value);

  if (isNaN(pcompra) || isNaN(pventa) || isNaN(smin) || isNaN(smax)) {
    toast('Todos los valores numéricos deben ser válidos.', 'error');
    return;
  }

  if (smax < smin) {
    toast('El stock máximo no puede ser inferior al stock mínimo.', 'error');
    return;
  }

  const payload = {
    precioCompra: pcompra,
    precioVenta: pventa,
    stockMinimo: smin,
    stockMaximo: smax
  };

  try {
    const res = await fetchWithAuth(`/api/inventario/variante/${varianteId}`, {
      method: 'PUT',
      body: payload
    });
    const data = await res.json();
    if (!res.ok) {
      toast(data.mensaje || 'Error actualizando variante', 'error');
      return;
    }

    toast(data.mensaje);
    cargarInventario();
    cargarCatalogoVariantes();
  } catch (err) {
    console.error(err);
    toast('Error al actualizar variante', 'error');
  }
}

async function guardarDatosGeneralesProductoSubmit(e) {
  if (e) e.preventDefault();
  const id = parseInt(document.getElementById('gp-edit-producto-id')?.value);
  const nombre = document.getElementById('gp-edit-nombre')?.value?.trim();
  const categoria = document.getElementById('gp-edit-categoria')?.value?.trim();
  const descripcion = document.getElementById('gp-edit-descripcion')?.value?.trim();

  if (!id || !nombre) {
    toast('El nombre de la prenda es obligatorio.', 'error');
    return;
  }

  const formData = new FormData();
  formData.append('nombre', nombre);
  formData.append('categoria', categoria);
  formData.append('descripcion', descripcion || '');
  formData.append('activo', 'true');
  const imagen = document.getElementById('gp-edit-imagen')?.files?.[0];
  if (imagen) formData.append('imagen', imagen);

  try {
    const res = await fetchWithAuth(`/api/inventario/producto/${id}`, {
      method: 'PUT',
      body: formData
    });
    const data = await res.json();
    if (!res.ok) {
      toast(data.mensaje || 'Error actualizando datos de la prenda', 'error');
      return;
    }

    toast(data.mensaje);
    await abrirModalGestionProducto(id);
    cambiarSubTabGestion('gptab-datos');
    cargarInventario();
    cargarCatalogoVariantes();
  } catch (err) {
    console.error(err);
    toast('Error al actualizar producto', 'error');
  }
}

async function eliminarProductoBaseActual() {
  const id = parseInt(document.getElementById('gp-edit-producto-id')?.value);
  const nombre = document.getElementById('gp-edit-nombre')?.value?.trim() || 'esta prenda';

  if (!id) {
    toast('No se encontró el ID del producto a eliminar.', 'error');
    return;
  }

  const confirmado = window.confirm(
    `¿Estás seguro de que deseas eliminar permanentemente el producto base "${nombre}" y todas sus variantes asociadas?\n\nEsta acción no se puede deshacer.`
  );
  if (!confirmado) return;

  try {
    const res = await fetchWithAuth(`/api/inventario/producto/${id}/eliminar`, {
      method: 'POST'
    });
    const data = await res.json().catch(() => ({}));

    if (!res.ok) {
      toast(data.mensaje || 'No se pudo eliminar el producto.', 'error');
      return;
    }

    toast(data.mensaje || 'Producto eliminado exitosamente.');
    cerrarModal('modal-gestionar-producto');
    cargarInventario();
    cargarCatalogoVariantes();
  } catch (err) {
    console.error('Error al eliminar producto:', err);
    toast('Error de conexión al intentar eliminar el producto.', 'error');
  }
}

function actualizarPrevisualizacionImagenGP() {
  const archivo = document.getElementById('gp-edit-imagen')?.files?.[0];
  const imgBox = document.getElementById('gp-header-img-box');
  if (imgBox) {
    if (archivo) {
      imgBox.innerHTML = `<img src="${URL.createObjectURL(archivo)}" alt="Previsualización" style="width:100%; height:100%; object-fit:cover;" />`;
    } else {
      imgBox.innerHTML = '<span style="font-size:26px;">👔</span>';
    }
  }
}

function renderHistorialEntradasGP(entradas) {
  const tbody = document.getElementById('gp-historial-table-body');
  if (!tbody) return;
  if (!entradas || entradas.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="7" style="text-align:center; padding:20px; color:var(--text-muted);">
          No se registran entradas de stock previas para esta prenda.
        </td>
      </tr>
    `;
    return;
  }

  tbody.innerHTML = entradas.map(e => `
    <tr>
      <td style="font-family:'JetBrains Mono'; font-size:11px;">${fmtDateTime(e.fecha)}</td>
      <td><strong>${e.talla}</strong> / ${e.color} <span style="font-size:10px; color:var(--text-muted);">(${e.sku})</span></td>
      <td><strong style="color:var(--green-accent); font-family:'JetBrains Mono';">+${e.cantidad} un.</strong></td>
      <td style="font-family:'JetBrains Mono'; color:var(--text-muted);">${e.stockAnterior} un.</td>
      <td style="font-family:'JetBrains Mono'; font-weight:700; color:var(--gold-bright);">${e.stockNuevo} un.</td>
      <td><span style="font-size:11px; color:var(--text-primary);">${e.responsable}</span></td>
      <td style="font-size:11px; color:var(--text-secondary);">${e.motivo || 'Entrada manual'}</td>
    </tr>
  `).join('');
}

function filtrarTablaInventario() {
  clearTimeout(debounceTimerInventario);
  debounceTimerInventario = setTimeout(() => {
    inventarioEstado.pagina = 1;
    cargarInventario(1);
  }, 250);
}

function abrirModalNuevoProducto() {
  document.getElementById('modal-nuevo-producto').classList.remove('hidden');
}

async function guardarNuevoProducto() {
  const codigoBase = document.getElementById('np-codigo').value.trim();
  const nombre = document.getElementById('np-nombre').value.trim();
  const categoria = document.getElementById('np-categoria').value.trim() || 'Prendas de Vestir';
  const descripcion = document.getElementById('np-descripcion').value.trim();

  const talla = document.getElementById('nv-talla').value.trim();
  const color = document.getElementById('nv-color').value.trim();
  const marca = document.getElementById('nv-marca').value.trim();
  const sku = document.getElementById('nv-sku').value.trim();
  const barras = document.getElementById('nv-barras').value.trim();
  const costo = parseFloat(document.getElementById('nv-costo').value) || 0;
  const venta = parseFloat(document.getElementById('nv-venta').value) || 0;
  const stock = parseInt(document.getElementById('nv-stock').value) || 0;

  if (!codigoBase || !nombre || !sku) {
    toast('Código base, nombre y SKU de la variante son obligatorios', 'error');
    return;
  }

  const formData = new FormData();
  formData.append('codigoBase', codigoBase);
  formData.append('nombre', nombre);
  formData.append('categoria', categoria);
  formData.append('descripcion', descripcion || '');
  formData.append('variantes[0].talla', talla || 'M');
  formData.append('variantes[0].color', color || 'Estándar');
  formData.append('variantes[0].marca', marca || 'Aisaac');
  formData.append('variantes[0].sku', sku);
  formData.append('variantes[0].codigoBarras', barras || '');
  formData.append('variantes[0].precioCompra', String(costo));
  formData.append('variantes[0].precioVenta', String(venta));
  formData.append('variantes[0].stockActual', String(stock));
  formData.append('variantes[0].stockMinimo', '5');
  formData.append('variantes[0].stockMaximo', '30');
  const imagen = document.getElementById('np-imagen')?.files?.[0];
  if (imagen) formData.append('imagen', imagen);

  try {
    const res = await fetchWithAuth('/api/inventario/producto', {
      method: 'POST',
      body: formData
    });
    const data = await res.json();
    if (!res.ok) {
      toast(data.mensaje || 'Error al guardar producto', 'error');
      return;
    }
    toast('¡Producto y variante agregados a PostgreSQL!');
    cerrarModal('modal-nuevo-producto');
    cargarInventario();
    cargarCatalogoVariantes();
  } catch (err) {
    toast('Error al comunicarse con la base de datos', 'error');
  }
}

// ═════════════════════════════════════════════════════════════════════════════
// 5. MÓDULO DE PROVEEDORES Y ENTRADAS DE MERCANCÍA
// ═════════════════════════════════════════════════════════════════════════════

async function cargarProveedores() {
  try {
    const res = await fetchWithAuth('/api/proveedores');
    if (!res.ok) return;
    const proveedores = await res.json();
    renderTablaProveedores(proveedores);

    // Poblar select de compras
    const select = document.getElementById('compra-proveedor-select');
    if (select) {
      select.innerHTML = proveedores.map(p => `
        <option value="${p.id}">${p.razonSocial} (${p.documentoNit})</option>
      `).join('');
    }
  } catch (err) {
    console.error('Error cargando proveedores:', err);
  }
}

function renderTablaProveedores(proveedores) {
  const tbody = document.getElementById('proveedores-body');
  if (!tbody) return;
  tbody.innerHTML = proveedores.map(p => `
    <tr>
      <td><span class="product-code">${p.documentoNit}</span></td>
      <td><strong>${p.razonSocial}</strong></td>
      <td>${p.contacto || '—'}</td>
      <td>${p.telefono || '—'}</td>
      <td>${p.email || '—'}</td>
      <td>${p.ciudad}</td>
      <td><span class="badge-stock normal">${p.totalCompras} compras</span></td>
    </tr>
  `).join('');
}

async function cargarCompras() {
  try {
    const res = await fetchWithAuth('/api/compras');
    if (!res.ok) return;
    const compras = await res.json();
    const tbody = document.getElementById('compras-body');
    if (!tbody) return;

    tbody.innerHTML = compras.map(c => `
      <tr>
        <td><span class="product-code">${c.numeroFactura}</span></td>
        <td>${fmtDate(c.fecha)}</td>
        <td><strong>${c.proveedorNombre}</strong></td>
        <td>${c.totalUnidades} prendas recibidas</td>
        <td style="font-family:'JetBrains Mono'; font-weight:600; color:var(--gold-bright)">${fmt(c.total)}</td>
        <td><span class="badge-stock normal">Ingresado al Stock</span></td>
      </tr>
    `).join('');
  } catch (err) {
    console.error('Error cargando compras:', err);
  }
}

function abrirModalNuevoProveedor() {
  document.getElementById('modal-nuevo-proveedor').classList.remove('hidden');
}

async function guardarNuevoProveedor() {
  const nit = document.getElementById('prov-nit').value.trim();
  const razon = document.getElementById('prov-razon').value.trim();
  if (!nit || !razon) {
    toast('NIT y Razón Social son requeridos', 'error');
    return;
  }

  const payload = {
    documentoNit: nit,
    razonSocial: razon,
    contacto: document.getElementById('prov-contacto').value.trim(),
    telefono: document.getElementById('prov-telefono').value.trim(),
    ciudad: document.getElementById('prov-ciudad').value.trim() || 'Neiva',
    email: document.getElementById('prov-email').value.trim(),
    direccion: document.getElementById('prov-direccion').value.trim()
  };

  try {
    const res = await fetchWithAuth('/api/proveedores', { method: 'POST', body: payload });
    const data = await res.json();
    if (!res.ok) { toast(data.mensaje || 'Error creando proveedor', 'error'); return; }
    toast('Proveedor registrado en PostgreSQL');
    cerrarModal('modal-nuevo-proveedor');
    cargarProveedores();
  } catch {
    toast('Error al registrar proveedor', 'error');
  }
}

function abrirModalNuevaCompra(preVarianteId = null) {
  actualizarSelectsFiltros();
  if (preVarianteId) {
    const select = document.getElementById('compra-variante-select');
    if (select) select.value = preVarianteId;
  }
  autoCompletarCostoCompra();
  toggleDescuentoCaja();
  actualizarTotalCompraModal();
  document.getElementById('modal-nueva-compra').classList.remove('hidden');
}

function autoCompletarCostoCompra() {
  const select = document.getElementById('compra-variante-select');
  if (!select) return;
  const opt = select.options[select.selectedIndex];
  if (opt) {
    const costo = opt.dataset.costo || 0;
    document.getElementById('compra-costo').value = costo;
    actualizarTotalCompraModal();
  }
}

function actualizarTotalCompraModal() {
  const cant = parseInt(document.getElementById('compra-cantidad')?.value) || 0;
  const costo = parseFloat(document.getElementById('compra-costo')?.value) || 0;
  const total = cant * costo;
  const el = document.getElementById('compra-total-calculado');
  if (el) el.textContent = fmt(total);
}

function toggleDescuentoCaja() {
  const medio = document.getElementById('compra-medio-pago')?.value;
  const wrap = document.getElementById('compra-caja-wrap');
  const chk = document.getElementById('compra-descontar-caja');
  if (medio === 'Efectivo') {
    if (wrap) wrap.style.display = 'flex';
    if (chk) chk.checked = true;
  } else {
    if (wrap) wrap.style.display = 'none';
    if (chk) chk.checked = false;
  }
}

async function guardarNuevaCompra() {
  const provId = parseInt(document.getElementById('compra-proveedor-select').value);
  const factura = document.getElementById('compra-factura').value.trim();
  const varId = parseInt(document.getElementById('compra-variante-select').value);
  const cant = parseInt(document.getElementById('compra-cantidad').value) || 0;
  const costo = parseFloat(document.getElementById('compra-costo').value) || 0;

  if (!provId || !varId || cant <= 0) {
    toast('Selecciona proveedor, prenda y cantidad válida', 'error');
    return;
  }

  const medioPago = document.getElementById('compra-medio-pago')?.value || 'Efectivo';
  const descontarCaja = document.getElementById('compra-descontar-caja')?.checked ?? true;

  const payload = {
    proveedorId: provId,
    numeroFactura: factura || `ENT-${Date.now().toString().slice(-6)}`,
    medioPago: medioPago,
    descontarCaja: descontarCaja,
    observaciones: document.getElementById('compra-obs')?.value?.trim() || '',
    items: [{
      varianteProductoId: varId,
      cantidad: cant,
      costoUnitario: costo
    }]
  };

  try {
    const res = await fetchWithAuth('/api/compras', { method: 'POST', body: payload });
    const data = await res.json();
    if (!res.ok) { toast(data.mensaje || 'Error registrando compra', 'error'); return; }
    toast(`¡Compra confirmada! Stock actualizado en PostgreSQL${payload.descontarCaja && payload.medioPago === 'Efectivo' ? ' y egreso descontado de la caja activa' : ''}.`);
    cerrarModal('modal-nueva-compra');
    cargarCompras();
    cargarInventario();
    cargarCatalogoVariantes();
    cargarDashboard();
    renderCierre();
  } catch {
    toast('Error procesando entrada de mercancía', 'error');
  }
}

// ═════════════════════════════════════════════════════════════════════════════
// 6. DASHBOARD GERENCIAL, ESTADÍSTICAS Y REABASTECIMIENTO
// ═════════════════════════════════════════════════════════════════════════════

async function cargarDashboard() {
  try {
    const res = await fetchWithAuth('/api/dashboard/resumen');
    if (!res.ok) return;
    const data = await res.json();

    // KPIs
    document.getElementById('kpi-ventas-mes').textContent = fmt(data.kpis.totalVentasMes);
    document.getElementById('kpi-ventas-hoy').textContent = fmt(data.kpis.totalVentasHoy);
    document.getElementById('kpi-valor-stock').textContent = fmt(data.kpis.valorInventarioVenta);
    document.getElementById('kpi-alertas-count').textContent = data.kpis.totalAlertasCriticas;
    renderPresupuestoProveedores(data.kpis);

    // 1. Top Prendas Más Vendidas por Variantes
    renderTopVendidas(data.topVendidas);

    // 2. Alertas Automáticas de Stock Mínimo y Reabastecimiento Sugerido
    renderAlertasStock(data.alertasStock);

    // 3. Distribución por Medio de Pago
    renderMediosDistribucion(data.ventasMedios);

    renderHistorialVentas(data.historialVentas || []);
  } catch (err) {
    console.error('Error cargando dashboard:', err);
  }
}

function renderTopVendidas(topList) {
  const container = document.getElementById('top-vendidas-container');
  if (!container) return;

  if (topList.length === 0) {
    container.innerHTML = '<div style="color:var(--text-muted); font-size:12px;">No hay ventas registradas aún para generar ranking.</div>';
    return;
  }

  const maxUnidades = Math.max(...topList.map(t => t.unidadesVendidas), 1);

  container.innerHTML = topList.map((item, idx) => {
    const porcentaje = Math.round((item.unidadesVendidas / maxUnidades) * 100);
    return `
      <div class="top-item-bar">
        <div class="top-item-info">
          <span class="top-item-name">#${idx + 1} ${item.nombrePrenda} — <span style="color:var(--gold-bright); font-size:11px;">${item.talla} / ${item.color} (${item.marca})</span></span>
          <span class="top-item-stat">${item.unidadesVendidas} un. vendidas · ${fmt(item.totalIngresos)}</span>
        </div>
        <div class="progress-track">
          <div class="progress-fill" style="width: ${porcentaje}%"></div>
        </div>
      </div>
    `;
  }).join('');
}

function renderAlertasStock(alertas) {
  const container = document.getElementById('alertas-stock-container');
  if (!container) return;

  if (alertas.length === 0) {
    container.innerHTML = `
      <div style="background:rgba(61,214,140,0.1); border:1px solid rgba(61,214,140,0.3); border-radius:8px; padding:14px; text-align:center; color:var(--green-accent);">
        ✓ ¡Excelente! Todas las variantes tienen existencias por encima del stock mínimo.
      </div>`;
    return;
  }

  container.innerHTML = alertas.map(a => {
    const badgeColor = a.nivelAlerta === 'AGOTADO' ? 'agotado' : (a.nivelAlerta === 'CRITICO' ? 'critico' : 'bajo');
    return `
      <div style="background:var(--bg-card-2); border:1px solid var(--border-subtle); border-radius:8px; padding:12px; display:flex; justify-content:space-between; align-items:center; gap:12px;">
        <div>
          <div style="display:flex; align-items:center; gap:8px;">
            <strong style="color:var(--text-primary); font-size:13px;">${a.nombreProducto}</strong>
            <span class="badge-stock ${badgeColor}">${a.nivelAlerta}</span>
            <span class="product-code">${a.sku}</span>
          </div>
          <div style="font-size:11px; color:var(--text-secondary); margin-top:3px;">
            Variante: ${a.talla} / ${a.color} (${a.marca}) · Stock actual: <strong style="color:${a.stockActual === 0 ? 'var(--red-accent)' : 'var(--gold-bright)'}">${a.stockActual}</strong> (Mínimo: ${a.stockMinimo})
          </div>
          <div style="font-size:11px; color:var(--gold-bright); margin-top:3px;">
            💡 Sugerencia Reabastecimiento: Pedir <strong>+${a.unidadesSugeridasReabastecer} unidades</strong> (Inversión estimada: ${fmt(a.costoEstimadoReabastecimiento)})
          </div>
        </div>
        <div>
          <button class="btn-primary" style="height:30px; font-size:11px; padding:0 10px;" onclick="abrirModalNuevaCompra(${a.id})">
            Reabastecer
          </button>
        </div>
      </div>
    `;
  }).join('');
}

function renderPresupuestoProveedores(kpis) {
  const fondo = Number(kpis.presupuestoProveedores || 0);
  const pagado = Number(kpis.comprasPagadasMes || 0);
  const disponible = Number(kpis.presupuestoDisponible || 0);
  const porcentaje = fondo > 0 ? Math.min(100, Math.round((pagado / fondo) * 100)) : 0;

  document.getElementById('kpi-proveedores-fondo').textContent = fmt(fondo);
  document.getElementById('kpi-proveedores-pagado').textContent = fmt(pagado);
  document.getElementById('kpi-proveedores-disponible').textContent = fmt(disponible);
  document.getElementById('provider-budget-progress-bar').style.width = `${porcentaje}%`;
}

async function cargarHistorialVentas() {
  const container = document.getElementById('ventas-historial-container');
  if (!container) return;
  container.innerHTML = '<div class="dashboard-empty">Cargando historial de ventas...</div>';

  const badgeFiltro = document.getElementById('historial-rol-badge');
  const textFiltro = document.getElementById('historial-alcance-text');
  const esCajero = state.usuario && state.usuario.rol === 'Cajero';

  if (badgeFiltro && textFiltro) {
    if (esCajero) {
      textFiltro.textContent = `Tus ventas registradas (${state.usuario.nombre})`;
    } else {
      textFiltro.textContent = 'Auditoría General (Todas las ventas)';
    }
  }

  try {
    const url = '/api/ventas/historial';
    const res = await fetchWithAuth(url);
    if (!res.ok) throw new Error('No se pudo cargar el historial');
    const ventas = await res.json();
    renderHistorialVentas(ventas);
  } catch (err) {
    console.error(err);
    container.innerHTML = '<div class="dashboard-empty">No se pudo cargar el historial de ventas.</div>';
  }
}

function renderHistorialVentas(ventas) {
  const container = document.getElementById('ventas-historial-container');
  if (!container) return;
  if (!ventas.length) {
    container.innerHTML = '<div class="dashboard-empty">No hay ventas registradas para este criterio de consulta.</div>';
    return;
  }

  const grupos = {};
  ventas.forEach(v => {
    const fecha = new Date(v.fecha);
    const clave = fecha.toLocaleDateString('es-CO');
    if (!grupos[clave]) grupos[clave] = [];
    grupos[clave].push(v);
  });

  container.innerHTML = Object.entries(grupos).map(([fecha, filas]) => {
    const totalDia = filas.reduce((sum, v) => sum + Number(v.total || 0), 0);
    const detalle = filas.map(v => {
      let medio = v.medioPago || 'Otro';
      if ((v.medioPagoCodigo || '').toUpperCase() === 'TRANSFERENCIA' && v.banco) medio += ` · ${v.banco}`;
      if (v.referenciaPago) medio += ` · Ref. ${v.referenciaPago}`;
      if ((v.medioPagoCodigo || '').toUpperCase() === 'CREDITO' && v.codigoAprobacion) medio += ` · Aprob. ${v.codigoAprobacion}`;
      const hora = fmtDateTime(v.fecha).split(' ')[1] || '';

      return `<div class="sale-history-row">
        <span>
          <b style="color:var(--text-primary); font-size:13px; font-family:'JetBrains Mono';">${v.numeroFactura}</b>
          <small style="color:var(--text-muted); font-size:11px;">${v.cajero} · ${hora}</small>
        </span>
        <span class="sale-payment-method">
          <span style="font-weight:600; color:var(--text-secondary);">${medio}</span>
          <small style="display:block; color:var(--text-muted); font-size:10px;">${v.totalPrendas || (v.detalles ? v.detalles.length : 1)} prenda(s)</small>
        </span>
        <strong style="font-family:'JetBrains Mono'; font-size:14px; color:var(--gold-bright); text-align:right;">${fmt(v.total)}</strong>
        <div style="text-align:right;">
          <button class="btn-ver-detalle-venta" onclick="verDetalleFactura(${v.id})" title="Ver detalle de prendas, precios y comprobante">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
            Ver detalle
          </button>
        </div>
      </div>`;
    }).join('');

    return `<details class="sale-history-day" open>
      <summary><span>${fecha}</span><strong>${filas.length} ventas · ${fmt(totalDia)}</strong></summary>
      <div class="sale-history-rows">${detalle}</div>
    </details>`;
  }).join('');
}

// ── VER DETALLE DE FACTURA / IMPRIMIR Y DESCARGAR PDF ──────────────────────────

async function verDetalleFactura(ventaId) {
  try {
    const res = await fetchWithAuth(`/api/ventas/${ventaId}`);
    if (!res.ok) {
      toast('No tienes permiso para ver esta factura o no existe.', 'error');
      return;
    }

    const data = await res.json();
    state.facturaSeleccionada = data;

    const titleEl = document.getElementById('mf-header-title');
    if (titleEl) titleEl.textContent = `Detalle de Factura #${data.numeroFactura}`;

    const printArea = document.getElementById('factura-imprimible-area');
    if (printArea) {
      let infoPagoExtra = '';
      if (data.categoriaPago === 'Transferencia') {
        infoPagoExtra = `
          <div style="display:flex; justify-content:space-between; margin-top:3px;">
            <span style="color:var(--text-muted);">Banco / Billetera:</span>
            <strong>${data.banco || 'Nequi / Daviplata'}</strong>
          </div>
          <div style="display:flex; justify-content:space-between; margin-top:3px;">
            <span style="color:var(--text-muted);">Comprobante / Ref:</span>
            <strong style="font-family:'JetBrains Mono'; color:var(--gold-bright);">${data.referenciaPago || '—'}</strong>
          </div>
        `;
      } else if (data.categoriaPago === 'Credito') {
        infoPagoExtra = `
          <div style="display:flex; justify-content:space-between; margin-top:3px;">
            <span style="color:var(--text-muted);">Código Aprobación:</span>
            <strong style="font-family:'JetBrains Mono'; color:var(--gold-bright);">${data.codigoAprobacion || '—'}</strong>
          </div>
        `;
      } else if (data.categoriaPago === 'Efectivo') {
        infoPagoExtra = `
          <div style="display:flex; justify-content:space-between; margin-top:3px;">
            <span style="color:var(--text-muted);">Efectivo Recibido:</span>
            <strong style="font-family:'JetBrains Mono';">${fmt(data.efectivoRecibido || data.total)}</strong>
          </div>
          <div style="display:flex; justify-content:space-between; margin-top:3px;">
            <span style="color:var(--text-muted);">Cambio Entregado:</span>
            <strong style="font-family:'JetBrains Mono'; color:var(--green-accent);">${fmt(data.cambioEntregado || 0)}</strong>
          </div>
        `;
      }

      const filasPrendas = data.detalles.map(d => `
        <tr style="border-bottom:1px solid rgba(255,255,255,0.06); font-size:12px;">
          <td style="padding:8px 4px;">
            <strong style="color:var(--text-primary); display:block;">${d.producto}</strong>
            <span style="font-size:10px; color:var(--text-muted); font-family:'JetBrains Mono';">${d.sku} · ${d.talla} / ${d.color} ${d.marca ? `(${d.marca})` : ''}</span>
          </td>
          <td style="text-align:center; padding:8px 4px; font-family:'JetBrains Mono'; font-weight:700;">${d.cantidad}</td>
          <td style="text-align:right; padding:8px 4px; font-family:'JetBrains Mono'; color:var(--text-secondary);">${fmt(d.precioUnitario)}</td>
          <td style="text-align:right; padding:8px 4px; font-family:'JetBrains Mono'; font-weight:700; color:var(--gold-bright);">${fmt(d.subtotal)}</td>
        </tr>
      `).join('');

      printArea.innerHTML = `
        <div style="text-align:center; padding-bottom:12px; border-bottom:1px dashed var(--border-gold); margin-bottom:14px;">
          <div style="font-size:18px; font-weight:900; color:var(--gold-bright); letter-spacing:1px;">AISAAC STR</div>
          <div style="font-size:11px; color:var(--text-secondary); letter-spacing:1px; margin-top:2px;">RETAIL & FASHION ERP</div>
          <div style="font-size:10px; color:var(--text-muted); margin-top:2px;">NIT: 900.123.456-7 · Régimen Simplificado</div>
          <div style="font-size:10px; color:var(--text-muted);">Cra. 8 # 4-52, Campoalegre - Huila · Tel: +57 312 456 7890</div>
          <div style="display:inline-block; margin-top:8px; padding:3px 12px; border-radius:12px; background:rgba(212,168,67,0.15); border:1px solid rgba(212,168,67,0.3); font-size:12px; font-weight:800; color:var(--gold-bright); font-family:'JetBrains Mono';">
            FACTURA ELECTRÓNICA DE VENTA: ${data.numeroFactura}
          </div>
        </div>

        <div style="font-size:11px; display:flex; flex-direction:column; gap:5px; margin-bottom:14px;">
          <div style="display:flex; justify-content:space-between;">
            <span style="color:var(--text-muted);">Fecha y Hora:</span>
            <strong style="font-family:'JetBrains Mono'; color:var(--text-primary);">${fmtDateTime(data.fecha)}</strong>
          </div>
          <div style="display:flex; justify-content:space-between;">
            <span style="color:var(--text-muted);">Responsable / Cajero:</span>
            <strong>${data.cajero}</strong>
          </div>
          <div style="display:flex; justify-content:space-between;">
            <span style="color:var(--text-muted);">Medio de Pago:</span>
            <strong style="color:var(--gold-bright);">${data.medioPago}</strong>
          </div>
          ${infoPagoExtra}
        </div>

        <div style="margin-bottom:14px;">
          <div style="font-size:11px; font-weight:700; color:var(--gold-bright); text-transform:uppercase; letter-spacing:1px; margin-bottom:6px; border-bottom:1px solid rgba(255,255,255,0.1); padding-bottom:4px;">
            Prendas Adquiridas (${data.detalles.reduce((acc, d) => acc + d.cantidad, 0)} unidades)
          </div>
          <table style="width:100%; border-collapse:collapse;">
            <thead>
              <tr style="font-size:10px; color:var(--text-muted); text-transform:uppercase; border-bottom:1px dashed rgba(255,255,255,0.15);">
                <th style="text-align:left; padding:4px 0;">Prenda / Ref</th>
                <th style="text-align:center; padding:4px 0;">Cant</th>
                <th style="text-align:right; padding:4px 0;">Precio</th>
                <th style="text-align:right; padding:4px 0;">Total</th>
              </tr>
            </thead>
            <tbody>
              ${filasPrendas}
            </tbody>
          </table>
        </div>

        <div style="border-top:1px dashed var(--border-gold); padding-top:10px; margin-bottom:14px; font-size:12px; display:flex; flex-direction:column; gap:5px;">
          <div style="display:flex; justify-content:space-between;">
            <span style="color:var(--text-muted);">Subtotal:</span>
            <span style="font-family:'JetBrains Mono';">${fmt(data.subtotal)}</span>
          </div>
          <div style="display:flex; justify-content:space-between;">
            <span style="color:var(--text-muted);">Descuento:</span>
            <span style="font-family:'JetBrains Mono';">${fmt(data.descuento || 0)}</span>
          </div>
          <div style="display:flex; justify-content:space-between;">
            <span style="color:var(--text-muted);">IVA (0% Régimen Simplificado):</span>
            <span style="font-family:'JetBrains Mono';">$ 0</span>
          </div>
          <div style="display:flex; justify-content:space-between; align-items:center; margin-top:6px; padding-top:6px; border-top:1px solid rgba(255,255,255,0.1);">
            <strong style="font-size:14px; color:var(--text-primary); text-transform:uppercase; letter-spacing:1px;">Total Cobrado:</strong>
            <strong style="font-family:'JetBrains Mono'; font-size:18px; color:var(--gold-bright);">${fmt(data.total)}</strong>
          </div>
        </div>

        <div style="text-align:center; font-size:10px; color:var(--text-muted); border-top:1px dashed rgba(255,255,255,0.1); padding-top:10px; line-height:1.4;">
          <div>¡Gracias por tu compra en Aisaac STR!</div>
          <div>Garantía de 30 días por defectos de confección presentando este comprobante.</div>
          <div>Software Validado FET - Rivera Huila</div>
        </div>
      `;
    }

    document.getElementById('modal-detalle-factura').classList.remove('hidden');
  } catch (err) {
    console.error(err);
    toast('Error consultando el detalle de la venta', 'error');
  }
}

function imprimirFacturaDetalle() {
  window.print();
}

async function descargarFacturaPDF() {
  const factura = state.facturaSeleccionada;
  if (!factura?.id) {
    toast('No hay una factura seleccionada.', 'error');
    return;
  }

  try {
    const res = await fetchWithAuth(`/api/ventas/${factura.id}/pdf`);
    if (!res.ok) throw new Error('No se pudo generar el PDF de la factura.');

    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const enlace = document.createElement('a');
    const nombre = String(factura.numeroFactura || factura.id).replace(/[<>:"/\\|?*]/g, '_');
    enlace.href = url;
    enlace.download = `Factura_${nombre}.pdf`;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    URL.revokeObjectURL(url);
  } catch (err) {
    console.error(err);
    toast(err.message || 'Error descargando la factura en PDF.', 'error');
  }
}

function renderMediosDistribucion(medios) {
  const container = document.getElementById('medios-distribucion-container');
  if (!container) return;

  const totalGral = medios.reduce((acc, m) => acc + m.total, 0) || 1;

  container.innerHTML = medios.map(m => {
    const pct = Math.round((m.total / totalGral) * 100);
    let icon = '💵';
    if (m.categoria === 'Transferencia') icon = '🏦';
    if (m.categoria === 'Credito') icon = '💳';

    return `
      <div style="background:var(--bg-card-2); border:1px solid var(--border-subtle); border-radius:8px; padding:14px;">
        <div style="display:flex; justify-content:space-between; align-items:center;">
          <span style="font-size:12px; font-weight:700; color:var(--gold-bright);">${icon} ${m.categoria.toUpperCase()}</span>
          <span style="font-size:11px; color:var(--text-muted);">${m.transacciones} trx</span>
        </div>
        <div style="font-size:18px; font-weight:800; font-family:'JetBrains Mono'; margin:6px 0;">${fmt(m.total)}</div>
        <div class="progress-track" style="height:6px;">
          <div class="progress-fill" style="width: ${pct}%;"></div>
        </div>
        <div style="font-size:10px; color:var(--text-muted); margin-top:4px;">${pct}% del volumen mensual</div>
      </div>
    `;
  }).join('');
}

// ── Modales Helpers ───────────────────────────────────────────────────────────
function cerrarModal(id) {
  document.getElementById(id).classList.add('hidden');
}

// ── Pestañas / Navegación ─────────────────────────────────────────────────────
function initTabs() {
  document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
      document.querySelectorAll('.panel').forEach(p => p.classList.remove('active'));

      btn.classList.add('active');
      const target = btn.dataset.panel;
      const targetPanel = document.getElementById(target);
      if (targetPanel) targetPanel.classList.add('active');

      // Mostrar el carrito ÚNICAMENTE en el módulo Punto de Venta (conservando productos seleccionados)
      actualizarVisibilidadCarrito(target);

      if (target === 'panel-cierre') renderCierre();
      if (target === 'panel-inventario') cargarInventario();
      if (target === 'panel-proveedores') { cargarProveedores(); cargarCompras(); }
      if (target === 'panel-dashboard') cargarDashboard();
      if (target === 'panel-historial') cargarHistorialVentas();
    });
  });

  // Asegurar estado de visibilidad inicial al cargar la aplicación
  const activeBtn = document.querySelector('.tab-btn.active');
  actualizarVisibilidadCarrito(activeBtn ? activeBtn.dataset.panel : 'panel-pdv');
}

function actualizarVisibilidadCarrito(targetPanel) {
  const btnCart = document.getElementById('btn-toggle-cart');
  if (!btnCart) return;

  if (targetPanel === 'panel-pdv') {
    btnCart.style.removeProperty('display');
  } else {
    btnCart.style.setProperty('display', 'none', 'important');
  }
}

// ── Reloj del Topbar ──────────────────────────────────────────────────────────
function startClock() {
  const el = document.getElementById('topbar-time');
  const update = () => { el.textContent = fmtDateTime(); };
  update();
  setInterval(update, 30000);
}

// ── Inicialización General al cargar el DOM ───────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  document.getElementById('modal-editar-usuario')
    ?.addEventListener('click', event => {
      if (event.target === event.currentTarget) cerrarModalEditarUsuario();
    });
  document.getElementById('modal-password-usuario')
    ?.addEventListener('click', event => {
      if (event.target === event.currentTarget) cerrarModalPasswordUsuario();
    });

  const themeToggle = document.getElementById('theme-toggle');

  const actualizarTema = () => {
    const isLight = document.documentElement.dataset.theme === 'light';
    if (!themeToggle) return;
    themeToggle.innerHTML = `<span class="theme-toggle-icon" aria-hidden="true">${isLight ? '☾' : '☀'}</span>`;
    themeToggle.setAttribute('aria-pressed', String(isLight));
    themeToggle.setAttribute('aria-label', isLight ? 'Cambiar a modo oscuro' : 'Cambiar a modo claro');
    themeToggle.title = isLight ? 'Cambiar a modo oscuro' : 'Cambiar a modo claro';
  };

  themeToggle?.addEventListener('click', () => {
    const nuevoTema = document.documentElement.dataset.theme === 'light' ? 'dark' : 'light';
    document.documentElement.dataset.theme = nuevoTema;
    localStorage.setItem('aisaac-theme', nuevoTema);
    actualizarTema();
  });
  actualizarTema();
  startClock();
  initTabs();
  initSearch();
  initPayMethods();
  document.getElementById('form-crear-usuario')
    ?.addEventListener('submit', crearUsuario);
  document.getElementById('form-editar-usuario')
    ?.addEventListener('submit', actualizarUsuario);
  document.getElementById('form-password-usuario')
    ?.addEventListener('submit', restablecerPasswordUsuario);
  document.getElementById('usuario-password')
    ?.addEventListener('input', event => event.target.setCustomValidity(''));
  const sanitizeNombre = event => {
    event.target.value = event.target.value.replace(/[^A-Za-zÀ-ÿ\s]/g, '');
    event.target.setCustomValidity('');
  };
  const sanitizeTelefono = event => {
    event.target.value = event.target.value.replace(/[^0-9+\s]/g, '').slice(0, 15);
    event.target.setCustomValidity('');
  };
  document.getElementById('usuario-nombre')?.addEventListener('input', sanitizeNombre);
  document.getElementById('usuario-telefono')?.addEventListener('input', sanitizeTelefono);
  document.getElementById('editar-usuario-nombre')?.addEventListener('input', sanitizeNombre);
  document.getElementById('editar-usuario-telefono')?.addEventListener('input', sanitizeTelefono);
  checkAuth();
});
