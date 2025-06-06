namespace PayrollSystem.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; }

        // Parameterless constructor for JSON deserialization
        public Department() { }

        public Department(int departmentId, string name)
        {
            DepartmentId = departmentId;
            Name = name;
        }

        public override string ToString()
        {
            return $"Department ID: {DepartmentId}, Name: {Name}";
        }
    }
}