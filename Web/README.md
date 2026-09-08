# Palabravo web

Landing bilingüe, páginas legales y API de eliminación para Azure Static Web Apps.

## Desarrollo local

Requiere Node.js 20 o superior.

```bash
npm install
npm run dev
```

Para compilar y probar el frontend:

```bash
npm run build
npm test
```

La API vive en `api/`:

```bash
cd api
npm install
npm run build
npm test
```

Para probar frontend y Functions juntos, copia `api/local.settings.example.json` como `api/local.settings.json`, reemplaza los valores de desarrollo y ejecuta Azure Static Web Apps CLI sobre el build de `Web`.

## Azure Static Web Apps

1. Crear una Azure Static Web App conectada al repositorio `rretamal/Palabravo`, rama `main`.
2. Guardar el token de despliegue como el secreto de GitHub `AZURE_STATIC_WEB_APPS_API_TOKEN`.
3. Configurar en Azure Static Web Apps → Configuration:

| Variable | Valor |
| --- | --- |
| `PLAYFAB_TITLE_ID` | `153ECF` |
| `PLAYFAB_SECRET_KEY` | Clave secreta del título PlayFab |
| `RESEND_API_KEY` | Clave de API de Resend |
| `RESEND_FROM` | `Palabravo <hello@palabravo.app>` |
| `PRIVACY_REQUEST_TO` | `hello@palabravo.app` |
| `PUBLIC_SITE_URL` | URL pública, por ejemplo `https://palabravo.app` |

La clave de PlayFab y la de Resend son secretos de ejecución: no deben agregarse a GitHub, al frontend ni a la app móvil.

## Dominio y correo

1. Agregar `palabravo.app` como dominio personalizado de Azure Static Web Apps.
2. Verificar `palabravo.app` en Resend y publicar sus registros SPF y DKIM.
3. Confirmar que `hello@palabravo.app` recibe correo antes de habilitar el formulario.
4. Probar una solicitud manual y revisar tanto la notificación administrativa como la confirmación al jugador.

Las solicitudes manuales no se guardan en una base de datos. Deben cerrarse dentro de 30 días y el correo asociado debe eliminarse 30 días después del cierre.
