# Use the .NET SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder
WORKDIR /app

# Copy project files and restore dependencies
COPY ApiLoadTester/*.csproj ./
RUN dotnet restore

# Copy all files and build
COPY ApiLoadTester/ ./
RUN dotnet publish -c Release -o out

# Create runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=builder /app/out .
ENTRYPOINT ["dotnet", "ApiLoadTester.dll"]
