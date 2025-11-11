# Enhancement Modules - Experimental Features

**Branch:** `enhancements-testing`  
**Safe Backup:** Tag `exam-stable-backup` on `master` branch

## Purpose
This folder contains experimental enhancement modules that extend Data Reviver's capabilities without modifying the original stable codebase.

## Modules

### Planned Enhancements
1. **FileCarvingEngine.cs** - Signature-based file recovery (works when MFT is corrupted)
2. **ParallelPrefetchScanner.cs** - Multi-threaded disk scanning (2-3x faster)
3. **EntropyAnalyzer.cs** - Content-based file type detection
4. **DeepMetadataValidator.cs** - Advanced validation to reduce false positives
5. **SlackSpaceRecovery.cs** - Recover data from unallocated sectors

## Integration Pattern
All enhancements use inheritance or extension pattern to avoid breaking original code:

```csharp
// Original remains untouched
var scanner = new Scanner(diskName, fileSystem);

// Optional enhancement (toggle in UI)
#if ENHANCEMENTS_ENABLED
var enhancedScanner = new EnhancedScanner(diskName, fileSystem);
enhancedScanner.EnableFileCarving = true;
#endif
```

## Testing Approach
- Original Scanner.cs: Unchanged
- Enhancement modules: Inherit from Scanner or use composition
- UI toggle: Enable/disable enhancements via checkbox

## Safety Notes
- **Never merge to master without testing**
- Original code remains 100% stable
- Can delete this entire folder without breaking anything
