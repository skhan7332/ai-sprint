import { defineConfig } from '@hey-api/openapi-ts'

export default defineConfig({
  input: 'http://localhost:5241/openapi/v1.json',
  output: 'src/api',
  plugins: ['@hey-api/typescript'],
})
