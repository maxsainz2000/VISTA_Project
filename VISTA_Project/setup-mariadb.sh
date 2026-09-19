#!/bin/bash
# Setup script to install and configure MariaDB for headless integration tests in Jules Ubuntu VM

# Exit on any error
set -e

echo "Updating apt cache..."
sudo apt-get update -y

echo "Installing MariaDB Server..."
curl -LsS https://r.mariadb.com/downloads/mariadb_repo_setup | sudo bash -s -- --mariadb-server-version="mariadb-11.4"
sudo apt-get install -y mariadb-server

echo "Starting MariaDB service..."
sudo systemctl start mariadb || sudo service mariadb start
sudo systemctl enable mariadb || true

echo "Configuring MariaDB user and databases..."
sudo mysql -e "CREATE DATABASE IF NOT EXISTS VISTA_DB;"
sudo mysql -e "CREATE USER IF NOT EXISTS 'root'@'127.0.0.1' IDENTIFIED BY '';"
sudo mysql -e "GRANT ALL PRIVILEGES ON *.* TO 'root'@'127.0.0.1';"
sudo mysql -e "FLUSH PRIVILEGES;"

echo "MariaDB setup complete."
