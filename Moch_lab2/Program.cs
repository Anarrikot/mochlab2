using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Drawing;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080"); // Указываем порт 8080
var app = builder.Build();

app.MapGet("/simulate", ([FromQuery] int sizeX = 100, [FromQuery] int sizeY = 100, [FromQuery] int sizeZ = 100, [FromQuery] double alpha = 0.5, [FromQuery] double dx = 1.0, [FromQuery] double dy = 1.0, [FromQuery] double dz = 1.0, [FromQuery] double dt = 0.1, [FromQuery] int steps = 1000) =>
{
    var simulation = new HeatSimulation(sizeX, sizeY, sizeZ, alpha, dx, dy, dz, dt, steps);

    var sw = Stopwatch.StartNew();
    simulation.SimulateSequential();
    sw.Stop();
    var sequentialTime = sw.ElapsedMilliseconds;

    sw.Restart();
    simulation.SimulateParallel();
    sw.Stop();
    var parallelTime = sw.ElapsedMilliseconds;

    return new
    {
        SequentialTime = sequentialTime,
        ParallelTime = parallelTime
    };
});

app.Run();

public class HeatSimulation
{
    private int sizeX, sizeY, sizeZ;
    private double[,,] temperature;
    private double[,,] newTemperature;
    private double alpha; // Коэффициент теплопроводности
    private double dx, dy, dz, dt;
    private int steps;

    public HeatSimulation(int sizeX, int sizeY, int sizeZ, double alpha, double dx, double dy, double dz, double dt, int steps)
    {
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;
        this.alpha = alpha;
        this.dx = dx;
        this.dy = dy;
        this.dz = dz;
        this.dt = dt;
        this.steps = steps;

        temperature = new double[sizeX, sizeY, sizeZ];
        newTemperature = new double[sizeX, sizeY, sizeZ];

        // Инициализация начальных условий
        InitializeTemperature();
    }

    public double GetTemperature(int x, int y, int z)
    {
        return temperature[x, y, z];
    }

    private void InitializeTemperature()
    {
        // Устанавливаем температуру на границах куба
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                {
                    if (x == sizeX - 1 || y == 0 || y == sizeY - 1 || z == 0 || z == sizeZ - 1)
                    {
                        temperature[x, y, z] = 300.0; // Граничные условия
                    }
                    else if (x == 0)
                    {
                        temperature[x, y, z] = 100.0;
                    }
                    else
                    {
                        temperature[x, y, z] = 0.0; // Внутренние точки
                    }
                }
    }

    public void SimulateSequential()
    {
        for (int step = 0; step < steps; step++)
        {
            for (int x = 1; x < sizeX - 1; x++)
                for (int y = 1; y < sizeY - 1; y++)
                    for (int z = 1; z < sizeZ - 1; z++)
                    {
                        double dTx = (temperature[x + 1, y, z] - 2 * temperature[x, y, z] + temperature[x - 1, y, z]) / (dx * dx);
                        double dTy = (temperature[x, y + 1, z] - 2 * temperature[x, y, z] + temperature[x, y - 1, z]) / (dy * dy);
                        double dTz = (temperature[x, y, z + 1] - 2 * temperature[x, y, z] + temperature[x, y, z - 1]) / (dz * dz);

                        newTemperature[x, y, z] = temperature[x, y, z] + alpha * dt * (dTx + dTy + dTz);
                    }

            // Обновляем температуру
            for (int x = 1; x < sizeX - 1; x++)
                for (int y = 1; y < sizeY - 1; y++)
                    for (int z = 1; z < sizeZ - 1; z++)
                    {
                        temperature[x, y, z] = newTemperature[x, y, z];
                    }
        }
    }

    public void SimulateParallel()
    {
        for (int step = 0; step < steps; step++)
        {
            Parallel.For(1, sizeX - 1, x =>
            {
                for (int y = 1; y < sizeY - 1; y++)
                    for (int z = 1; z < sizeZ - 1; z++)
                    {
                        double dTx = (temperature[x + 1, y, z] - 2 * temperature[x, y, z] + temperature[x - 1, y, z]) / (dx * dx);
                        double dTy = (temperature[x, y + 1, z] - 2 * temperature[x, y, z] + temperature[x, y - 1, z]) / (dy * dy);
                        double dTz = (temperature[x, y, z + 1] - 2 * temperature[x, y, z] + temperature[x, y, z - 1]) / (dz * dz);

                        newTemperature[x, y, z] = temperature[x, y, z] + alpha * dt * (dTx + dTy + dTz);
                    }
            });

            // Обновляем температуру
            Parallel.For(1, sizeX - 1, x =>
            {
                for (int y = 1; y < sizeY - 1; y++)
                    for (int z = 1; z < sizeZ - 1; z++)
                    {
                        temperature[x, y, z] = newTemperature[x, y, z];
                    }
            });
        }
    }

    public void VisualizeTemperature(string file_name)
    {
        Bitmap bitmap = new Bitmap(sizeX, sizeY);

        // Определяем срез по оси Z (середина куба)
        int zSlice = sizeZ / 2;

        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
            {
                // Температура на срезе z = zSlice
                double temp = temperature[x, y, zSlice];

                // Преобразуем температуру в цвет
                int colorValue = (int)(255 * (temp / 100.0));
                colorValue = Math.Min(255, Math.Max(0, colorValue));
                bitmap.SetPixel(x, y, Color.FromArgb(colorValue, 0, 255 - colorValue));
            }

        // Сохраняем изображение
        bitmap.Save(file_name);
    }
}