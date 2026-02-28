// ============================================================
// Runtime Configuration
// ============================================================
// DOCKER: This file is overwritten by docker-entrypoint.sh at startup
//         using API_BASE_URL and PAT_TTL_MINUTES environment variables
//
// IIS: For Windows IIS deployment, manually edit this file BEFORE
//      building to set the correct backend API URL
//      Example IIS: 'http://localhost/PIPlanningBackend'
//      Example IIS with IP: 'http://192.168.1.100/PIPlanningBackend'
//
// Local Dev: Update apiBaseUrl to match your backend port
//            (default: 5262 from launchSettings.json)
// ============================================================

window['__env'] = window['__env'] || {};
window['__env']['apiBaseUrl'] = 'http://localhost:5262';
window['__env']['patTtlMinutes'] = '10';
