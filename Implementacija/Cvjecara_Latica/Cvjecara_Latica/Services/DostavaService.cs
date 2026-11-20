namespace Cvjecara_Latica.Services
{
    public class DostavaService
    {
        public double IzracunajCijenuDostave(double iznosNarudzbe, string grad)
        {
            if (iznosNarudzbe <= 0)
                throw new ArgumentOutOfRangeException(nameof(iznosNarudzbe), "Iznos narudžbe mora biti veći od 0.");
            if (string.IsNullOrWhiteSpace(grad))
                throw new ArgumentException("Grad dostave mora biti unesen.", nameof(grad));
            if (grad == "Sarajevo")
                return 5.0;

            return 10.0;
        }
    }
}