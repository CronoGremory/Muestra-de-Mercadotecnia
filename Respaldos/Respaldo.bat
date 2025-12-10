@echo off
:: Obtener fecha y hora para el nombre del archivo
set FECHA=%date:~-4,4%-%date:~-7,2%-%date:~-10,2%_%time:~0,2%-%time:~3,2%
set FECHA=%FECHA: =0%

echo Iniciando respaldo de la tabla NUMEROS...

:: Comando SQLPlus para guardar los datos en un archivo CSV
(
echo SET HEADING OFF;
echo SET ECHO OFF;
echo SET PAGESIZE 0;
echo SET FEEDBACK OFF;
echo SET COLSEP ',';
echo SELECT * FROM NUMEROS;
echo EXIT;
) | sqlplus -s MUESTRA_ADMIN/Muestra.2025@localhost:1521/XEPDB1 > "C:\Users\luisp_xjn0yu1\Desktop\Muestra\Respaldos\backup_%FECHA%.csv"

echo Respaldo completado.