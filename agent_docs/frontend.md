# FMS Frontend Architecture

## Module Federation Setup

### Shell App (Host)
```typescript
// webpack.config.js
const { shareAll, withModuleFederationPlugin } = require('@angular-architects/module-federation/webpack');

module.exports = withModuleFederationPlugin({
  remotes: {
    logistics: 'logistics@http://localhost:4201/remoteEntry.js',
    taxi: 'taxi@http://localhost:4202/remoteEntry.js',
    'school-bus': 'schoolBus@http://localhost:4203/remoteEntry.js',
    ambulance: 'ambulance@http://localhost:4204/remoteEntry.js',
    'public-transport': 'publicTransport@http://localhost:4205/remoteEntry.js',
    mining: 'mining@http://localhost:4206/remoteEntry.js',
    railways: 'railways@http://localhost:4207/remoteEntry.js',
    'law-enforcement': 'lawEnforcement@http://localhost:4208/remoteEntry.js',
  },
  shared: {
    ...shareAll({ singleton: true, strictVersion: true, requiredVersion: 'auto' }),
  },
});
```

### Dynamic Module Loading
```typescript
async loadRemoteModule(moduleName: string) {
  const manifest = await this.http.get('/api/v1/config/modules').toPromise();
  if (!manifest[moduleName]?.enabled) {
    throw new Error(`Module ${moduleName} not enabled for tenant`);
  }
  return await loadRemoteModule({
    type: 'module',
    remoteEntry: manifest[moduleName].remoteEntry,
    exposedModule: './Module',
  });
}
```

## Dynamic Table Component
- Reads column config from `user_preferences` API
- Features: show/hide, reorder (drag-drop), resize, sort, filter, page size
- Saves preferences per user per page

## Dynamic Form Renderer
- Accepts JSON Schema, renders Angular Reactive Forms
- Supports: text, number, dropdown, date, checkbox, device-picker, etc.

## White-Label Theming
- Fetches branding config from `/api/v1/config/branding`
- Injects CSS variables into :root at runtime
- Supports: primary color, secondary color, logo URL, favicon, font family

## SignalR Service
- Connects to `/hubs/fleet`
- Joins groups: `${tenantId}:${deviceId}` for each tracked device
- Handles: telemetry updates, alerts, command acknowledgments
- Auto-reconnect with exponential backoff

## i18n & RTL
- Dynamic locale loading from `/assets/i18n/{lang}.json`
- Sets document direction: `dir="ltr" | dir="rtl"`
- Supported: en, es, fr, ar, he, hi, zh

## UAT Frontend URL
- **Shell App:** `https://fms-web-uat.vercel.app`
- **Platform Admin:** `https://fms-admin-uat.vercel.app`
