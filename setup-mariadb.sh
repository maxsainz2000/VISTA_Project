#!/bin/bash
# Setup script to install and configure MariaDB for headless integration tests in Jules Ubuntu VM

# Exit on any error
set -e

echo "Updating apt cache..."
sudo apt-get update -y

echo "Installing MariaDB Server..."
sudo apt-get install -y mariadb-server

echo "Starting MariaDB service..."
sudo systemctl start mariadb
sudo systemctl enable mariadb

echo "Configuring MariaDB user and databases..."
sudo mysql -e "CREATE DATABASE IF NOT EXISTS VISTA_DB;"
sudo mysql -e "CREATE USER IF NOT EXISTS 'root'@'localhost' IDENTIFIED BY '';"
sudo mysql -e "GRANT ALL PRIVILEGES ON *.* TO 'root'@'localhost';"
sudo mysql -e "FLUSH PRIVILEGES;"

echo "MariaDB setup complete."
