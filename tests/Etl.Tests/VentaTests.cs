using Xunit;
using Etl.Application.Services;
using Etl.Domain.Entities;

namespace Etl.Tests;

internal static class TestData
{
    public static VentaRecord Venta(string factura = "F-1", short linea = 1, int cantidad = 1,
        decimal monto = 100m, string fuente = "CSV") =>
        new(factura, linea, new DateTime(2026, 1, 15), "P001", "Teclado", "Accesorios", null, null, null, null,
            "C1", "Ana PÃ©rez", "Individual", null, "RepÃºblica Dominicana", "N/D", "Santo Domingo",
            null, null, null, cantidad, monto, 50m, fuente);
}

public class VentaValidatorTests
{
    [Fact]
    public void VentaCorrecta_NoTieneMotivoDeRechazo() =>
        Assert.Null(VentaValidator.Validar(TestData.Venta()));

    [Fact]
    public void CantidadCero_SeRechaza() =>
        Assert.Equal("cantidad debe ser mayor que 0", VentaValidator.Validar(TestData.Venta(cantidad: 0)));

    [Fact]
    public void MontoNegativo_SeRechaza() =>
        Assert.Equal("monto_total negativo", VentaValidator.Validar(TestData.Venta(monto: -1m)));

    [Fact]
    public void FuenteDesconocida_SeRechaza() =>
        Assert.Equal("origen_fuente inválido", VentaValidator.Validar(TestData.Venta(fuente: "EXCEL")));
}

public class VentaConsolidatorTests
{
    [Fact]
    public void ConLaMismaClave_GanaLaFuenteDeMayorPrioridad()
    {
        IReadOnlyList<VentaRecord> api = new[] { TestData.Venta("F-1", 1, monto: 200m, fuente: "API") };
        IReadOnlyList<VentaRecord> csv = new[]
        {
            TestData.Venta("F-1", 1, monto: 100m, fuente: "CSV"),
            TestData.Venta("F-2", 1, fuente: "CSV")
        };

        var resultado = VentaConsolidator.Consolidar(new[] { api, csv });

        Assert.Equal(2, resultado.Registros.Count);
        Assert.Equal(1, resultado.Descartados);
        Assert.Equal(200m, resultado.Registros.Single(v => v.NumeroFactura == "F-1").MontoTotal);
    }

    [Fact]
    public void LineasDistintasDeLaMismaFactura_NoSeDuplican()
    {
        IReadOnlyList<VentaRecord> csv = new[]
        {
            TestData.Venta("F-1", 1),
            TestData.Venta("F-1", 2)
        };

        var resultado = VentaConsolidator.Consolidar(new[] { csv });

        Assert.Equal(2, resultado.Registros.Count);
        Assert.Equal(0, resultado.Descartados);
    }
}

