namespace Cvjecara_Latica.Services
{
    /// <summary>
    /// Računa cijenu dostave narudžbe na osnovu iznosa i grada.
    /// Razvijeno TDD pristupom (Task 3).
    /// </summary>
    public class DostavaService
    {
        public double IzracunajCijenuDostave(double iznosNarudzbe, string grad)
        {
            if (iznosNarudzbe <= 0)
                throw new ArgumentOutOfRangeException(nameof(iznosNarudzbe), "Iznos narudžbe mora biti veći od 0.");
            if (string.IsNullOrWhiteSpace(grad))
                throw new ArgumentException("Grad dostave mora biti unesen.", nameof(grad));
            throw new NotImplementedException();
        }
    }
}