# FMS Device Adapter Framework

## MQTT Topic Pattern
```
{tenantId}/{vendorCode}/{deviceId}/FROM   ← Device → Server
{tenantId}/{vendorCode}/{deviceId}/CMD    ← Server → Device
```

Examples:
- `acme-logistics/itriangle/8679590332xxxx/FROM`
- `acme-logistics/streamax/8634560123xxxx/FROM`
- `acme-logistics/teltonika/3520930812xxxx/FROM`

## Standard Telemetry Model
```json
{
  "tenantId": "uuid",
  "deviceId": "uuid",
  "vendorCode": "itriangle",
  "timestamp": "2026-08-28T04:15:00Z",
  "latitude": 12.9716,
  "longitude": 77.5946,
  "speed": 65.5,
  "heading": 180,
  "ignition": true,
  "odometer": 45231.7,
  "fuelLevel": 78.5,
  "temperature": 32.0,
  "alerts": [
    { "type": "overspeed", "threshold": 60, "actual": 65.5, "severity": "high" }
  ],
  "rawVendorPayload": { }
}
```

## Adding a New Vendor (Zero Code)
1. Admin uploads JSON schema to `device_vendors.schema_config`
2. System auto-generates:
   - MQTT topic pattern
   - API ingestion endpoint (if HTTP)
   - Field mapping rules
   - Command mapping
3. Tenants can immediately select vendor and add devices

## Two-Way Commands
| Command | iTriangle | Streamax | Teltonika |
|---------|-----------|----------|-----------|
| Immobilize | TCP hex `0x01 0x10 0x00` | MQTT `{"cmd":"lock"}` | TCP `setdigout 1` |
| Mobilize | TCP hex `0x01 0x10 0x01` | MQTT `{"cmd":"unlock"}` | TCP `setdigout 0` |
| Poll | TCP hex `0x01 0x20 0x00` | MQTT `{"cmd":"status"}` | TCP `getstatus` |
| Camera Snap | N/A | MQTT `{"cmd":"capture"}` | N/A |
| Video Clip | N/A | MQTT `{"cmd":"clip","start":"...","duration":60}` | N/A |

## Device Health Monitoring
- Online/offline status (last_seen < 5 min = online)
- Signal strength (dBm)
- Battery level (%)
- GPS satellite count
- Network type (4G/3G/2G)
- Latency (ms)
