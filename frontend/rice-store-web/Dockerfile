# ---------- Build the Angular app ----------
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# ---------- Serve with nginx ----------
FROM nginx:1.27-alpine AS final
# Angular build output goes to dist/rice-store-web/browser in modern Angular.
COPY --from=build /app/dist/rice-store-web/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY docker-entrypoint.sh /docker-entrypoint.d/99-write-config.sh
RUN chmod +x /docker-entrypoint.d/99-write-config.sh
EXPOSE 80
