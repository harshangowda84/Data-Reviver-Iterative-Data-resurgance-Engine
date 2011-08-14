namespace KFA.Disks {
    public interface IImageable : IHasSectors {
        Attributes GetAttributes();
    }
}
