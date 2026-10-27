using System;
using System.IO;
namespace TheTechIdea.Beep.DataBase
{
 // Counts actual encoded JSON bytes without retaining sensitive request contents or an unbounded buffer.
 internal sealed class BoundedMutationEncodingStream : Stream
 {
  private long _length;
  private readonly long _limit;
  internal BoundedMutationEncodingStream(long limit) { _limit=limit; }
  public override bool CanRead => false;
  public override bool CanSeek => false;
  public override bool CanWrite => true;
  public override long Length => _length;
  public override long Position { get=>_length; set=>throw new NotSupportedException("The encoding counter is not seekable."); }
  public override void Flush() { }
  public override void Write(byte[] buffer,int offset,int count)
  {
   ArgumentNullException.ThrowIfNull(buffer);
   if(offset<0||count<0||offset>buffer.Length-count)throw new ArgumentException("The encoding buffer range is invalid.");
   Write(buffer.AsSpan(offset,count));
  }
  public override void Write(ReadOnlySpan<byte> buffer)
  { if(buffer.Length>_limit-_length)throw new ArgumentException("The complete document exceeds its encoded transaction byte budget.");_length+=buffer.Length; }
  public override int Read(byte[] buffer,int offset,int count)=>throw new NotSupportedException("The encoding counter is write-only.");
  public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException("The encoding counter is not seekable.");
  public override void SetLength(long value)=>throw new NotSupportedException("The encoding counter length follows actual writes.");
 }
}
