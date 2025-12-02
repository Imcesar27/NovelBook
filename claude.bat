@echo off
REM ============================================
REM Claude Code - Asistente para proyecto NovelBook
REM Ubicación: C:\Users\Cesar\Source\Repos\NovelBook
REM ============================================

echo.
echo ====================================================
echo       CLAUDE CODE - Proyecto NovelBook
echo ====================================================
echo.

REM Verificar que estamos en el directorio correcto
cd /d C:\Users\Cesar\Source\Repos\NovelBook

REM Mostrar el directorio actual
echo Directorio del proyecto: %CD%
echo.

REM Si no se proporcionan argumentos, mostrar menú
if "%~1"=="" goto :menu

REM Si hay argumentos, ejecutar Claude Code directamente
echo Ejecutando Claude Code...
echo.
npx @anthropic-ai/claude-code %*
goto :end

:menu
echo Que deseas hacer?
echo.
echo 1. Revisar y mejorar codigo existente
echo 2. Agregar comentarios y documentacion
echo 3. Buscar y corregir errores
echo 4. Agregar nueva funcionalidad
echo 5. Optimizar rendimiento
echo 6. Escribir comando personalizado
echo 7. Salir
echo.

set /p opcion="Selecciona una opcion (1-7): "

if "%opcion%"=="1" goto :revisar
if "%opcion%"=="2" goto :documentar
if "%opcion%"=="3" goto :debug
if "%opcion%"=="4" goto :nueva_funcion
if "%opcion%"=="5" goto :optimizar
if "%opcion%"=="6" goto :personalizado
if "%opcion%"=="7" goto :salir

echo Opcion invalida. Intenta de nuevo.
pause
goto :menu

:revisar
echo.
echo Revisando codigo del proyecto NovelBook...
npx @anthropic-ai/claude-code "Revisa el codigo del proyecto NovelBook, identifica areas de mejora, posibles problemas y sugiere mejoras manteniendo el estilo actual del codigo"
goto :end

:documentar
echo.
echo Agregando documentacion al codigo...
npx @anthropic-ai/claude-code "Agrega comentarios descriptivos en español al codigo del proyecto NovelBook, explicando que hace cada funcion y seccion importante para que un principiante pueda entenderlo"
goto :end

:debug
echo.
echo Buscando errores en el proyecto...
npx @anthropic-ai/claude-code "Busca errores, warnings y posibles problemas en el codigo del proyecto NovelBook. Si encuentras algo, corrigelo y explica que cambiaste"
goto :end

:nueva_funcion
echo.
set /p funcionalidad="Describe la funcionalidad que quieres agregar: "
echo.
echo Agregando nueva funcionalidad...
npx @anthropic-ai/claude-code "Agrega esta funcionalidad al proyecto NovelBook: %funcionalidad%. Manten el mismo estilo de codigo y diseño existente"
goto :end

:optimizar
echo.
echo Optimizando el proyecto...
npx @anthropic-ai/claude-code "Analiza y optimiza el rendimiento del codigo en el proyecto NovelBook. Mejora la eficiencia sin cambiar la funcionalidad existente"
goto :end

:personalizado
echo.
set /p comando="Escribe tu comando para Claude: "
echo.
echo Ejecutando comando personalizado...
npx @anthropic-ai/claude-code "%comando%"
goto :end

:salir
echo.
echo Cerrando Claude Code...
exit

:end
echo.
echo ====================================================
echo Tarea completada!
echo ====================================================
echo.
echo Presiona cualquier tecla para cerrar...
pause > nul