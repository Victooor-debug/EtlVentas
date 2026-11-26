using Etl.Application;
using Xunit;

namespace Etl.Tests;

public class DimensionTests
{
    [Fact]
    public void BuildTiempo_DeduplicaFechasYCalculaAtributos()
    {
        var rows = new[]
        {
            new DimensionSourceRow(new DateTime(2026, 10, 15), "P1", "Prod", "Cat", "Sub", "M", 100, 50, "C1", "Cli", "Corp", "a@b.com", "RD", "Este", "SD", "API", "V1", "Juan", "Este"),
            new DimensionSourceRow(new DateTime(2026, 10, 15), "P2", "Prod2", "Cat", "Sub", "M", 200, 80, "C1", "Cli", "Corp", "a@b.com", "RD", "Este", "SD", "CSV", "V1", "Juan", "Este")
        };

        var tiempo = DimensionMapper.BuildTiempo(rows);

        Assert.Single(tiempo);
        Assert.Equal(20261015, tiempo[0].TiempoId);
        Assert.Equal("Octubre", tiempo[0].NombreMes);
        Assert.Equal(4, tiempo[0].Trimestre);
    }

    [Fact]
    public void BuildClientesYProductos_DeduplicaPorCodigo()
    {
        var rows = new[]
        {
            new DimensionSourceRow(DateTime.Today, "P01", "Laptop", "Tech", "PC", "MSI", 1000, 700, "C01", "Victor", "Retail", "v@test.com", "RD", "Este", "SD", "API", "V01", "Ana", "Este"),
            new DimensionSourceRow(DateTime.Today, "p01", "Laptop", "Tech", "PC", "MSI", 1000, 700, "c01", "Victor", "Retail", "v@test.com", "RD", "Este", "SD", "CSV", "V01", "Ana", "Este")
        };

        Assert.Single(DimensionMapper.BuildClientes(rows));
        Assert.Single(DimensionMapper.BuildProductos(rows));
        Assert.Single(DimensionMapper.BuildVendedores(rows));
    }
}
