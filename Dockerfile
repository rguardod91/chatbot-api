# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copiamos los fuentes y publicamos
COPY . .
RUN dotnet publish "src/ChatBot.Api/ChatBot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# 1. CAMBIO CRÍTICO: Crear la carpeta y copiar los certificados AQUÍ
USER root 
RUN mkdir -p /certs
COPY /https/chatbot/chatbot.crt /certs/chatbot.crt
COPY /https/chatbot/chatbot.key /certs/chatbot.key
# Si necesitas la CA interna para llamadas salientes:
COPY /https/ca/internal-ca.crt /usr/local/share/ca-certificates/
RUN chmod 644 /certs/chatbot.crt /certs/chatbot.key && update-ca-certificates

# 2. Copiar la aplicación publicada
COPY --from=build /app/publish .

# 3. Configuración de entorno
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/certs/chatbot.crt
ENV ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/certs/chatbot.key
ENV ASPNETCORE_URLS=https://+:22260
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV TZ=America/Panama

# Importante: Si la imagen aspnet:10.0 usa el usuario 'app' por defecto, 
# asegúrate de que tenga permisos sobre los certs
RUN chown -R app:app /certs
USER app

EXPOSE 22260
ENTRYPOINT ["dotnet", "ChatBot.Api.dll"]