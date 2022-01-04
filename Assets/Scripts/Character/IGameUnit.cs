using GameManagement;

namespace Character
{
    public interface IGameUnit
    {
        public int NetworkID { get; set; }
        public GameData.Team Team { get; set; }
    
        public float MaxHealth { get; set; }
        public float Health { get; set; }
        public float MoveSpeed { get; set; }
        public float RotationSpeed { get; set; }
    
        public float AttackDamage { get; set; }
        public float AttackSpeed { get; set; }
    
        public float AttackRange { get; set; }
    
    }
}
