# Build Angular app
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build --prod

# Serve with nginx
FROM nginx:alpine
COPY --from=build /app/dist/pi-planning-ui /usr/share/nginx/html
EXPOSE 80
