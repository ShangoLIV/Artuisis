using System.Collections.Generic;
using Caillou;

[System.Serializable]
public class SwarmData
{
    #region Private fields
    private List<AgentData> agentsData;
    private SwarmParameters parameters;
    private SerializableRandom random;
    private List<TokenData> tokens = new List<TokenData>(); 
    #endregion

    #region Methods - Constructor
    public SwarmData(List<AgentData> agentsData, SwarmParameters parameters, List<TokenData> tokens = null)
    {
        this.agentsData = agentsData;
        this.parameters = parameters;
        this.random = new SerializableRandom();
        this.tokens = tokens != null ? CloneTokens(tokens) : new List<TokenData>();
    }

    public SwarmData(List<AgentData> agentsData, SwarmParameters parameters, SerializableRandom random, List<TokenData> tokens = null)
    {
        this.agentsData = agentsData;
        this.parameters = parameters;
        this.random =  random;
        this.tokens = tokens != null ? CloneTokens(tokens) : new List<TokenData>();
    }
    #endregion

    #region Methods - Getter
    public List<AgentData> GetAgentsData() => new List<AgentData>(agentsData);

    public IReadOnlyList<TokenData> GetTokens() => tokens;
    
    public SwarmParameters GetParameters()      => parameters;
    public SerializableRandom GetRandomGenerator() => random;
    
    #endregion
    
    #region Clone
    public SwarmData Clone()
    {
        var agentsClone = new List<AgentData>();
        foreach (var a in agentsData) agentsClone.Add(a.Clone());
        
        return new SwarmData(agentsClone, parameters.Clone(), random.Clone(), tokens);
    }
    #endregion

    #region Methods - Setter
    public void SetParameters(SwarmParameters parameters) => this.parameters = parameters;
    
    public void SetTokens(IEnumerable<TokenData> snapshot) // --- MOD v1: nouveau setter pratique
    {
        tokens = CloneTokens(snapshot);
    }
    
    #endregion
    
    #region Helpers
    private static List<TokenData> CloneTokens(IEnumerable<TokenData> src)
    {
        var list = new List<TokenData>();
        if (src == null) return list;
        foreach (var t in src)
            list.Add(new TokenData(t.Polarity, t.Position,
                t.Range, t.Strength01, t.HitRadius));
        return list;
    }
    #endregion
}
