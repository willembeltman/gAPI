using gAPI.Storage.LanCloud.Shared.Dtos;

namespace gAPI.Storage.LanCloud.Api.Models;

internal record RespondedEntry(
    FileSystemEntry FileSystemEntry,
    HubEntryDto ShareEntryDto, 
    string Path);
