#!/bin/bash

#Clonar los repositorios de Recon-USB-Pass
git clone https://github.com/Recon-USB-Pass/RUSBP-Agent.git
git clone https://github.com/Recon-USB-Pass/RUSBP-BackEnd.git
git clone https://github.com/Recon-USB-Pass/RUSBP-FrontEnd.git
git clone https://github.com/Recon-USB-Pass/RUSBP-DataBase.git
git clone https://github.com/Recon-USB-Pass/RUSBP-Infraestructure.git
git clone https://github.com/Recon-USB-Pass/RUSBP-Testing.git
git clone https://github.com/Recon-USB-Pass/RUSBP-Documentation.git

# Configura tu usuario y email en Git
git config --global user.name "zaratechacana"
git config --global user.email "zaratechacana@gmail.com"

# Lista de repositorios clonados
repos=(
    "RUSBP-Agent"
    "RUSBP-BackEnd"
    "RUSBP-FrontEnd"
    "RUSBP-DataBase"
    "RUSBP-Infraestructure"
    "RUSBP-Testing"
    "RUSBP-Documentation"
)

# Configurar cada repositorio
cd ..

for repo in "${repos[@]}"; do
    echo "Configurando $repo..."
    cd "$repo" || continue
    git remote set-url origin "https://github.com/Recon-USB-Pass/$repo.git"
    git branch --set-upstream-to=origin/main main
    cd ..
done

echo "Todos los repositorios han sido configurados."
