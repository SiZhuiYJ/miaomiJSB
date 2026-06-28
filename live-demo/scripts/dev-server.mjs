import { createServer } from 'node:http'
import { createReadStream } from 'node:fs'
import { stat } from 'node:fs/promises'
import { extname, join, normalize } from 'node:path'

const root = process.cwd()
const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8' }

createServer(async (request, response) => {
  const url = new URL(request.url ?? '/', 'http://localhost')
  const safePath = normalize(url.pathname).replace(/^\.\.(\/|\\|$)/, '')
  const filePath = join(root, safePath === '/' ? 'index.html' : safePath)
  try {
    const info = await stat(filePath)
    response.setHeader('Content-Type', types[extname(filePath)] ?? 'application/octet-stream')
    createReadStream(info.isDirectory() ? join(filePath, 'index.html') : filePath).pipe(response)
  } catch {
    response.statusCode = 404
    response.end('Not found')
  }
}).listen(5173, '0.0.0.0', () => console.log('live-demo dev server: http://localhost:5173'))
