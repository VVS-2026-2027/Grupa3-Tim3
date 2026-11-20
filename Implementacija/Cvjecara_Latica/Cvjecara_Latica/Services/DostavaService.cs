namespace Cvjecara_Latica.Services
{
    public class DostavaService
    {
         public double IzracunajCijenuDostave(double iznosNarudzbe, string grad)
        {
            if (iznosNarudzbe <= 0)
                throw new ArgumentOutOfRangeException(nameof(iznosNarudzbe), "Iznos narudžbe mora biti veći od 0.");

            throw new NotImplementedException();
        }
    }
}