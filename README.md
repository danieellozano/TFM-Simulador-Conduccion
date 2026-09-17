# 🚗 Simulador de Conducción Interactivo 3D orientado a la Formación Vial

[![Unity](https://img.shields.io/badge/Unity-6000.3.11f1%20(Unity%206)-black?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Pipeline-URP-blue)](https://unity.com/srp/universal-render-pipeline)
[![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS-lightgrey)](#)
[![Tests](https://img.shields.io/badge/Tests-58%20Passing%20(100%25)-brightgreen)](#-pruebas-y-aseguramiento-de-la-calidad)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)
[![Academic](https://img.shields.io/badge/TFM-Universidad%20de%20Granada%20(ETSIIT)-red)](https://etsiit.ugr.es/)

> **Trabajo de Fin de Máster (TFM)**  
> **Máster Universitario en Desarrollo de Software**  
> **Escuela Técnica Superior de Ingenierías Informática y de Telecomunicación (ETSIIT)**  
> **Universidad de Granada (UGR)**  
> **Autor:** Daniel Lozano Moya 

---

## 📌 Descripción del Proyecto

Este proyecto constituye un **simulador de conducción interactivo 3D de código abierto y bajo coste**, diseñado específicamente como una herramienta pedagógica para la instrucción y entrenamiento de conductores noveles. 

Frente a las soluciones comerciales cerradas y de elevado coste de adquisición, esta plataforma unifica un **motor de simulación física y cinemática vehicular fidedigno**, un **ecosistema de tráfico urbano inteligente (IA)** y un **motor de auditoría normativa en tiempo real** que evalúa el desempeño del alumno aplicando el baremo oficial de faltas de la **Dirección General de Tráfico (DGT)** de España, generando automáticamente actas formales de evaluación en formato HTML/PDF.

---

## ✨ Características Principales

### 🏎️ Dinámica Física y Tren Motriz
* **Física Vehicular con NVIDIA PhysX:** Simulación de suspensiones, transferencias dinámicas de masa y fricción longitudinal/lateral sobre `WheelCollider`.
* **Transmisión Manual Realista:** Desmultiplicación por marchas (1.ª a 5.ª + Marcha Atrás), desacoplamiento progresivo por embrague y retención por freno de motor (*engine braking*).
* **Mecánica de Calado y Arranque Seguro:** Detección de caída de régimen por bajo par a baja velocidad y protocolo de seguridad para el rearranque.
* **Transmisión Automática Secuencial:** Transición automática de marchas asistida por umbrales de RPM en conformidad con la normativa del Reglamento General de Conductores.

### 🚦 Tráfico Autónomo Reactivo (IA Multiagente)
* **Control Cinemático sobre Splines:** Tráfico fluido guiado por grafos de curvas paramétricas sin sobrecarga computacional.
* **Percepción Frontal por Raycasting:** Frenado adaptativo y mantenimiento de distancias de seguridad diferenciadas (vehículos vs. infraestructura).
* **Gobernador de Velocidad en Curvas:** Cálculo analítico de tangentes vectoriales para reducir la velocidad hasta un 50 % en tramos revirados.
* **Gestión de Intersecciones y Señales:** Prioridad en cruces no semaforizados mediante productos escalares y colas FIFO secuenciales para señales de STOP.

### 📋 Auditoría Normativa DGT en Tiempo Real
* **Catálogo de 31 Infracciones Parametrizadas:** Detección vectorial y espacial de faltas (STOP, semáforos, sentido contrario, límites de velocidad, líneas continuas, acoso a 2 s, colisiones, uso de intermitentes y freno de mano).
* **Baremo Oficial Examinador:** Cálculo del veredicto **APTO / NO APTO** en tiempo real (1 Eliminatoria, 2 Deficientes, 1 Deficiente + 5 Leves, o 10 Leves).
* **Filtro de Enfriamiento (*Cooldown*):** Supresión de falsos positivos y descarte de eventos repetitivos por persistencia física.
* **Generación de Actas HTML:** Serialización de resultados, historial cronológico de penalizaciones y exportación para consulta e impresión.

### 🖥️ Interfaz de Usuario y Ergonomía HMI
* **Dashboard Digital:** Velocímetro normalizado, indicador de marchas (R, N, 1-5), límite de vía e indicador de RPM con alerta visual crítica en rojo a $> 5500\text{ RPM}$.
* **Retrovisores Virtuales:** Tres espejos con texturas de renderizado en tiempo real (*Render Textures*) para fomentar patrones de exploración visual.
* **Navegación Asistida GPS:** Flecha direccional dinámica que calcula el ángulo relativo hacia el siguiente nodo de la ruta calculada por *NavMesh*.

---

## 🏗️ Arquitectura de Software

El simulador implementa el patrón **Núcleo y Satélites** estructurado bajo una **Arquitectura basada en ScriptableObjects (SOA)** y un **Bus de Eventos Asíncrono**, garantizando la separación estricta entre la lógica y los datos:

```
                          ┌───────────────────────┐
                          │   INPUT (HAL)         │
                          └───────────┬───────────┘
                                      │ Eventos & Contratos
                          ┌───────────▼───────────┐
┌───────────────────────┐ │                       │ ┌───────────────────────┐
│  PHYSICS (Vehículo)   │◄┼─►     CORE (SOA)     ◄┼─┤  AI (Tráfico)         │
└───────────────────────┘ │   (Pizarra de Datos)  │ └───────────────────────┘
                          │   (Bus de Eventos)    │
┌───────────────────────┐ │                       │ ┌───────────────────────┐
│  EVALUATION (DGT)     │◄┼─►                     ◄┼─┤  HUD & UI             │
└───────────────────────┘ └───────────┬───────────┘ └───────────────────────┘
                                      │
                          ┌───────────▼───────────┐
                          │ INFRASTRUCTURE        │
                          └───────────────────────┘
```

* **`Core`:** Orquestación del ciclo de vida, variables de estado desacopladas y distribución de eventos.
* **`Input`:** Capa de Abstracción de Hardware (HAL) que normaliza señales de teclado, mandos y volantes comerciales.
* **`Physics`:** Cinemática del automóvil, motor, embrague y frenado distribuido.
* **`AI`:** Agentes autónomos, SplineLinks, reguladores de cruce y colas de STOP.
* **`Evaluation`:** Sensores de calzada y chasis, baremo oficial DGT y exportador de informes.
* **`HUD`:** Cuadro de instrumentos, retrovisores virtuales y guía GPS.
* **`Infrastructure`:** Semáforos temporizados (FSM) y señales de velocidad inteligentes.

---

## 🎮 Esquema de Controles por Defecto (Teclado)

| Acción de Conducción | Tecla | Función durante la Simulación |
| :--- | :---: | :--- |
| **Acelerar** | `W` | Aplica aceleración progresiva al vehículo |
| **Frenar** | `S` | Acciona el freno de servicio para detener el coche |
| **Dirección (Girar)** | `A` / `D` | Gira la dirección a la izquierda (`A`) o derecha (`D`) |
| **Embrague** | `Shift Izq.` | Desacopla la transmisión (obligatorio para cambiar de marcha en manual) |
| **Subir Marcha** | `E` | Introduce la marcha superior (requiere embrague pisado $> 70\%$) |
| **Bajar Marcha** | `Q` | Reduce a la marcha inferior (requiere embrague pisado $> 70\%$) |
| **Freno de Mano** | `Espacio` | Conmuta el freno de estacionamiento (*Toggle*) |
| **Intermitentes** | `Z` / `X` | Enciende/apaga el indicador de giro izquierdo (`Z`) o derecho (`X`) |
| **Mirar a los Lados** | `←` / `→` | Rota horizontalmente la cámara del piloto dentro de la cabina |
| **Centrar Vista** | `↑` | Restablece suavemente la vista al frente de la calzada |
| **Arrancar Motor** | `R` | Reenciende el motor tras un calado accidental |
| **Pausa / Menú** | `P` | Congela la escala temporal del simulador y despliega el menú |

> **Compatibilidad con Hardware Profesional:** La arquitectura HAL permite enlazar directamente volantes de $900^\circ$ con *Force Feedback* y pedaleras triples USB (Logitech G29/G920/G923, Thrustmaster, Fanatec) mediante el mapa de acciones de Unity.

---

## ⚙️ Requisitos del Sistema

| Componente | Requisitos Mínimos (Estimados) | Plataforma Validada (Recomendada) |
| :--- | :--- | :--- |
| **Sistema Operativo** | Windows 10 (64-bit) / macOS 12+ | Windows 11 (64-bit) |
| **Procesador (CPU)** | Intel Core i5 / AMD Ryzen 5 (4 núcleos $\ge 2.5\text{ GHz}$) | Intel Core i7-10750H / AMD Ryzen 7 (8 núcleos) |
| **Tarjeta Gráfica (GPU)** | GPU DirectX 11 (NVIDIA GTX 1050 / AMD RX 560) | NVIDIA GeForce RTX 2060 / GTX 1660 Ti (6 GB VRAM) |
| **Memoria RAM** | 8 GB DDR4 | 16 GB DDR4 (3200 MHz) |
| **Almacenamiento** | 2 GB de espacio libre | SSD NVMe M.2 |

---

## 🚀 Instalación y Despliegue

### Abrir y Desarrollar en Unity Editor

#### 1. Clonar el repositorio con Git LFS (Obligatorio)
Es imprescindible tener instalado e inicializado [Git LFS](https://git-lfs.com/) para descargar correctamente las mallas 3D, texturas y modelos binarios:
```bash
# Instalar e inicializar Git LFS
git lfs install

# Clonar el repositorio
git clone https://github.com/danieellozano/TFM-SimuladorConduccion.git
```

#### 2. Abrir en Unity Hub
1. Abre **Unity Hub** y añade el proyecto seleccionando la carpeta raíz clonada.
2. Abre el proyecto con la versión **Unity 6 (6000.3.11f1 o superior)**.

#### 3. Generar el Mapa de Iluminación (Lightmapping)
Para reconstruir las luces horneadas locales en tu máquina:
* En la barra superior de Unity, navega a: `Window` $\to$ `Rendering` $\to$ `Lighting`.
* En la pestaña *Scene*, pulsa el botón **`Generate Lighting`**.

#### 4. Ejecutar la Simulación
* En la ventana *Project*, navega a `_Project/Scenes/` y abre la escena **`0_Menu_Principal.unity`**.
* Pulsa el botón **`Play (▶)`** en la parte superior del editor.

---

## 🧪 Pruebas y Aseguramiento de la Calidad

El proyecto cuenta con una suite integral de **58 pruebas automatizadas** desarrolladas sobre **Unity Test Framework (NUnit)**:

```
📁 Test Runner (EditMode & PlayMode)
├── 🟢 AITests.cs (6 tests)           -> Cinemática, distancias de parada, colas STOP y gobernadores
├── 🟢 CoreTests.cs (6 tests)         -> ScriptableObjects, Pizarra de datos, Observer y Misiones
├── 🟢 EvaluationTests.cs (16 tests)  -> Baremo DGT (1E/2D/10L), Cooldown, Sensores, Velocidad y Actas
├── 🟢 HUDTests.cs (8 tests)          -> Normalización de telemetría, alertas RPM, GPS y Menús
├── 🟢 InfrastructureTests.cs (6 tests)-> FSM Semáforos, Exclusión mutua e inyección de velocidad
├── 🟢 InputTests.cs (8 tests)        -> HAL, normalización analógica, bypass automático y eventos
├── 🟢 PhysicsTests.cs (8 tests)      -> Tren motriz, relaciones de marcha, calado y cámara
└── 🟢 VehiclePhysicsPlayMode (7 tests)-> Bucle dinámico en tiempo real (WaitForFixedUpdate)
```
> **Resultado de la Suite:** `58/58 Tests Pasados con Éxito (100 % de Aprobación)`.

---

## 📊 Métricas de Rendimiento (*Profiling*)

Auditoría de rendimiento obtenida con **Unity Profiler** y **Profiler Analyzer** sobre una muestra de **2000 fotogramas continuos** en el escenario urbano con tráfico activo:

* **Tasa de Refresco Media:** $\approx 135.40\text{ FPS}$ (Tiempo de fotograma medio: $7.39\text{ ms}$).
* **Consistencia Gráfica:** Mediana de $7.32\text{ ms}$ y rango intercuartílico de solo $0.70\text{ ms}$ (ausencia total de *jittering* o tirones).
* **Cumplimiento de Fluidez:** El **$99.80\%$** de los fotogramas se mantuvo por encima de los $60\text{ FPS}$ ($< 16.6\text{ ms}$).
* **Eficiencia de Memoria:** Tasa de asignación dinámica de **$0.6\text{ KB/frame}$** en el *Heap*, eliminando pausas por recolección de basura (*Garbage Collection*).

---

## 🎓 Cita Académica

Si utilizas este proyecto o su código en investigaciones académicas, por favor cita esta memoria:

```bibtex
@mastersthesis{lozano2026simulador,
  author  = {Daniel Lozano Moya},
  title   = {Desarrollo de un Simulador de Conducción orientado a la Formación de Conductores},
  school  = {Escuela Técnica Superior de Ingenierías Informática y de Telecomunicación, Universidad de Granada},
  year    = {2026},
  type    = {Trabajo Fin de Máster},
  month   = {Septiembre},
  address = {Granada, España}
}
```

---

## 📄 Licencia

Este proyecto está distribuido bajo la licencia **MIT**. Consulta el archivo [`LICENSE`](LICENSE) para más detalles.

---

