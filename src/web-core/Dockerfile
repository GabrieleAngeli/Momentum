FROM node:20 as build
WORKDIR /app
COPY package.json package-lock.json* yarn.lock* ./
RUN npm install
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /app/dist/web-core /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
