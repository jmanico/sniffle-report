FROM node:20.19-alpine AS install
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

FROM install AS build
WORKDIR /app

COPY . .

ARG VITE_API_URL=http://localhost:5000/api/v1
ENV VITE_API_URL=$VITE_API_URL

RUN npm run build

FROM nginx:1.27-alpine AS runtime
WORKDIR /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist ./

EXPOSE 5173

CMD ["nginx", "-g", "daemon off;"]
