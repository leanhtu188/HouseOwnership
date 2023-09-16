namespace ConsoleApp
{
    public class Relationship
    {
        public Person Principal { get; }
        public Person Dependent { get; }

        public RelationshipType Type { get; }

        private Relationship(Person principal, Person dependent, RelationshipType type)
        {
            Principal = principal;
            Dependent = dependent;
            Type = type;
        }


    }

    public enum RelationshipType
    {
        ParentChild,
        GuardianWard,
        Adoptive,
        Spouse
    }
}
