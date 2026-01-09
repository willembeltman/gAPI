using System;
using System.IO;

namespace gAPI.AutoSerialiser;

public class SerializerInstance<T>
{
    public SerializerInstance(
        Action<BinaryWriter, T> writeDelegate,
        Func<BinaryReader, T> readDelegate,
        string code)
    {
        WriteDelegate = writeDelegate;
        ReadDelegate = readDelegate;
        Code = code;
    }

    private readonly Action<BinaryWriter, T> WriteDelegate;
    private readonly Func<BinaryReader, T> ReadDelegate;
    public readonly string Code;

    /// <summary>
    /// Serializes the entity and writes it to the BinaryWriter (bw)
    /// </summary>
    /// <param name="bw">The BinaryWriter bw where the entity should be written to.</param>
    /// <param name="entity">The entity to write to the BinaryWriter bw.</param>
    /// <param name="dbContext">The DbContext needed to instanciate/get any sub-entity EntityFactories.</param>
    public void Write(BinaryWriter bw, T entity)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        WriteDelegate(bw, entity);
    }
    /// <summary>
    /// Instanciates and reads the next entity from the BinaryReader br.
    /// </summary>
    /// <param name="br">The BinaryReader br where the next entity should read from.</param>
    /// <param name="dbContext">The DbContext needed to instanciate/get any sub-entity EntityFactories.</param>
    /// <returns>The entity read from the BinaryReader</returns>
    public T Read(BinaryReader br)
    {
        return ReadDelegate(br);
    }
}
