# Dockerfile for Chat Client with Nginx
FROM nginx:alpine

# Copy static files
COPY index.html /usr/share/nginx/html/
COPY styles.css /usr/share/nginx/html/
COPY script.js /usr/share/nginx/html/
COPY ui.js /usr/share/nginx/html/
COPY attachments.js /usr/share/nginx/html/
COPY storage.js /usr/share/nginx/html/
COPY websocket.js /usr/share/nginx/html/

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]