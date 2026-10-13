// Serve the built fixture without hot reloading: node test/fields/serve.cjs
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../../dist/field-ui-test');
const types = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.map': 'application/json' };
http.createServer((request, response) => {
    const pathname = decodeURIComponent(new URL(request.url, 'http://127.0.0.1').pathname);
    const filename = path.resolve(root, `.${pathname === '/' ? '/index.html' : pathname}`);
    if (!filename.startsWith(root + path.sep)) { response.writeHead(404).end(); return; }
    fs.readFile(filename, (error, data) => {
        if (error) { response.writeHead(404).end(); return; }
        response.writeHead(200, { 'Content-Type': types[path.extname(filename)] ?? 'application/octet-stream' });
        response.end(data);
    });
}).listen(1256, '127.0.0.1', () => console.log('Static fixture: http://127.0.0.1:1256'));
