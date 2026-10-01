# Image de production Yakkomom (Render). Construite depuis la racine du dépôt.

# --- Compilation -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restauration séparée : la couche est réutilisée tant que le .csproj ne change pas
COPY Yakkomom/Yakkomom.csproj Yakkomom/
RUN dotnet restore Yakkomom/Yakkomom.csproj
COPY Yakkomom/ Yakkomom/
RUN dotnet publish Yakkomom/Yakkomom.csproj -c Release -o /app --no-restore

# --- Exécution -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Fuseau Africa/Dakar, et Kerberos que Npgsql tente de charger au démarrage ;
# ICU (formats et comparaisons en français) est déjà dans l'image
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production \
    TZ=Africa/Dakar \
    PORT=10000
EXPOSE 10000
# Utilisateur non root fourni par l'image .NET
USER $APP_UID
ENTRYPOINT ["dotnet", "Yakkomom.dll"]
