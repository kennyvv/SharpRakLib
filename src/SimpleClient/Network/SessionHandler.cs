using MiNET.Net;
using MiNET.Utils;
using SharpRakLib.Core;
using SharpRakLib.Core.Client;
using SharpRakLib.Server;

namespace SimpleClient.Network
{
    public class SessionHandler : IMcpeClientMessageHandler
    {
        private ClientSession Session { get; }
        private Program.PacketHook Client { get; }


        public SessionHandler(Program.PacketHook client, ClientSession session)
        {
            Client = client;
            Session = session;
        }

        public void HandleMcpePlayStatus(McpePlayStatus message)
        {
            Client.PlayerStatus = message.status;

            if (Client.PlayerStatus == 3)
            {
                Client.SendMcpeMovePlayer();
            }
        }

        public void HandleMcpeServerToClientHandshake(McpeServerToClientHandshake message)
        {
            
        }

        public void HandleMcpeDisconnect(McpeDisconnect message)
        {
            Session.Disconnect(message.message);
        }

        public void HandleMcpeResourcePacksInfo(McpeResourcePacksInfo message)
        {
            McpeResourcePackClientResponse response = new McpeResourcePackClientResponse();
            response.responseStatus = 3;
            Client.SendPacket(response);
        }

        public void HandleMcpeResourcePackStack(McpeResourcePackStack message)
        {
            McpeResourcePackClientResponse response = new McpeResourcePackClientResponse();
            response.responseStatus = 4;
            Client.SendPacket(response);
        }

        public void HandleMcpeText(McpeText message)
        {
            
        }

        public void HandleMcpeSetTime(McpeSetTime message)
        {
            
        }

        public void HandleMcpeStartGame(McpeStartGame message)
        {
            Client.EntityId = message.runtimeEntityId;
            Client.NetworkEntityId = message.entityIdSelf;
            
            Client.CurrentLocation = new PlayerLocation(message.spawn, message.unknown1.X, message.unknown1.X, message.unknown1.Y);
            
            var packet = McpeRequestChunkRadius.CreateObject();
            //Client.ChunkRadius = 5;
            packet.chunkRadius = 5;

            Client.SendPacket(packet);
        }

        public void HandleMcpeAddPlayer(McpeAddPlayer message)
        {
            
        }

        public void HandleMcpeAddEntity(McpeAddEntity message)
        {
            
        }

        public void HandleMcpeRemoveEntity(McpeRemoveEntity message)
        {
            
        }

        public void HandleMcpeAddItemEntity(McpeAddItemEntity message)
        {
            
        }

        public void HandleMcpeTakeItemEntity(McpeTakeItemEntity message)
        {
            
        }

        public void HandleMcpeMoveEntity(McpeMoveEntity message)
        {
            
        }

        public void HandleMcpeMovePlayer(McpeMovePlayer message)
        {
            
        }

        public void HandleMcpeRiderJump(McpeRiderJump message)
        {
            
        }

        public void HandleMcpeUpdateBlock(McpeUpdateBlock message)
        {
            
        }

        public void HandleMcpeAddPainting(McpeAddPainting message)
        {
            
        }

        public void HandleMcpeExplode(McpeExplode message)
        {
            
        }

        public void HandleMcpeLevelSoundEventOld(McpeLevelSoundEventOld message)
        {
            
        }

        public void HandleMcpeLevelEvent(McpeLevelEvent message)
        {
            
        }

        public void HandleMcpeBlockEvent(McpeBlockEvent message)
        {
            
        }

        public void HandleMcpeEntityEvent(McpeEntityEvent message)
        {
            
        }

        public void HandleMcpeMobEffect(McpeMobEffect message)
        {
            
        }

        public void HandleMcpeUpdateAttributes(McpeUpdateAttributes message)
        {
            
        }

        public void HandleMcpeInventoryTransaction(McpeInventoryTransaction message)
        {
            
        }

        public void HandleMcpeMobEquipment(McpeMobEquipment message)
        {
            
        }

        public void HandleMcpeMobArmorEquipment(McpeMobArmorEquipment message)
        {
            
        }

        public void HandleMcpeInteract(McpeInteract message)
        {
            
        }

        public void HandleMcpeHurtArmor(McpeHurtArmor message)
        {
            
        }

        public void HandleMcpeSetEntityData(McpeSetEntityData message)
        {
            
        }

        public void HandleMcpeSetEntityMotion(McpeSetEntityMotion message)
        {
            
        }

        public void HandleMcpeSetEntityLink(McpeSetEntityLink message)
        {
            
        }

        public void HandleMcpeSetHealth(McpeSetHealth message)
        {
            
        }

        public void HandleMcpeSetSpawnPosition(McpeSetSpawnPosition message)
        {
            
        }

        public void HandleMcpeAnimate(McpeAnimate message)
        {
            
        }

        public void HandleMcpeRespawn(McpeRespawn message)
        {
            
        }

        public void HandleMcpeContainerOpen(McpeContainerOpen message)
        {
            
        }

        public void HandleMcpeContainerClose(McpeContainerClose message)
        {
            
        }

        public void HandleMcpePlayerHotbar(McpePlayerHotbar message)
        {
            
        }

        public void HandleMcpeInventoryContent(McpeInventoryContent message)
        {
            
        }

        public void HandleMcpeInventorySlot(McpeInventorySlot message)
        {
            
        }

        public void HandleMcpeContainerSetData(McpeContainerSetData message)
        {
            
        }

        public void HandleMcpeCraftingData(McpeCraftingData message)
        {
            
        }

        public void HandleMcpeCraftingEvent(McpeCraftingEvent message)
        {
            
        }

        public void HandleMcpeGuiDataPickItem(McpeGuiDataPickItem message)
        {
            
        }

        public void HandleMcpeAdventureSettings(McpeAdventureSettings message)
        {
            
        }

        public void HandleMcpeBlockEntityData(McpeBlockEntityData message)
        {
            
        }

        public void HandleMcpeLevelChunk(McpeLevelChunk message)
        {
            
        }

        public void HandleMcpeSetCommandsEnabled(McpeSetCommandsEnabled message)
        {
            
        }

        public void HandleMcpeSetDifficulty(McpeSetDifficulty message)
        {
            
        }

        public void HandleMcpeChangeDimension(McpeChangeDimension message)
        {
            
        }

        public void HandleMcpeSetPlayerGameType(McpeSetPlayerGameType message)
        {
            
        }

        public void HandleMcpePlayerList(McpePlayerList message)
        {
            
        }

        public void HandleMcpeSimpleEvent(McpeSimpleEvent message)
        {
            
        }

        public void HandleMcpeTelemetryEvent(McpeTelemetryEvent message)
        {
            
        }

        public void HandleMcpeSpawnExperienceOrb(McpeSpawnExperienceOrb message)
        {
            
        }

        public void HandleMcpeClientboundMapItemData(McpeClientboundMapItemData message)
        {
            
        }

        public void HandleMcpeMapInfoRequest(McpeMapInfoRequest message)
        {
            
        }

        public void HandleMcpeRequestChunkRadius(McpeRequestChunkRadius message)
        {
            
        }

        public void HandleMcpeChunkRadiusUpdate(McpeChunkRadiusUpdate message)
        {
            
        }

        public void HandleMcpeItemFrameDropItem(McpeItemFrameDropItem message)
        {
            
        }

        public void HandleMcpeGameRulesChanged(McpeGameRulesChanged message)
        {
            
        }

        public void HandleMcpeCamera(McpeCamera message)
        {
            
        }

        public void HandleMcpeBossEvent(McpeBossEvent message)
        {
            
        }

        public void HandleMcpeShowCredits(McpeShowCredits message)
        {
            
        }

        public void HandleMcpeAvailableCommands(McpeAvailableCommands message)
        {
            
        }

        public void HandleMcpeCommandOutput(McpeCommandOutput message)
        {
            
        }

        public void HandleMcpeUpdateTrade(McpeUpdateTrade message)
        {
            
        }

        public void HandleMcpeUpdateEquipment(McpeUpdateEquipment message)
        {
            
        }

        public void HandleMcpeResourcePackDataInfo(McpeResourcePackDataInfo message)
        {
            
        }

        public void HandleMcpeResourcePackChunkData(McpeResourcePackChunkData message)
        {
            
        }

        public void HandleMcpeTransfer(McpeTransfer message)
        {
            
        }

        public void HandleMcpePlaySound(McpePlaySound message)
        {
            
        }

        public void HandleMcpeStopSound(McpeStopSound message)
        {
            
        }

        public void HandleMcpeSetTitle(McpeSetTitle message)
        {
            
        }

        public void HandleMcpeAddBehaviorTree(McpeAddBehaviorTree message)
        {
            
        }

        public void HandleMcpeStructureBlockUpdate(McpeStructureBlockUpdate message)
        {
            
        }

        public void HandleMcpeShowStoreOffer(McpeShowStoreOffer message)
        {
            
        }

        public void HandleMcpePlayerSkin(McpePlayerSkin message)
        {
            
        }

        public void HandleMcpeSubClientLogin(McpeSubClientLogin message)
        {
            
        }

        public void HandleMcpeInitiateWebSocketConnection(McpeInitiateWebSocketConnection message)
        {
            
        }

        public void HandleMcpeSetLastHurtBy(McpeSetLastHurtBy message)
        {
            
        }

        public void HandleMcpeBookEdit(McpeBookEdit message)
        {
            
        }

        public void HandleMcpeNpcRequest(McpeNpcRequest message)
        {
            
        }

        public void HandleMcpeModalFormRequest(McpeModalFormRequest message)
        {
            
        }

        public void HandleMcpeServerSettingsResponse(McpeServerSettingsResponse message)
        {
            
        }

        public void HandleMcpeShowProfile(McpeShowProfile message)
        {
            
        }

        public void HandleMcpeSetDefaultGameType(McpeSetDefaultGameType message)
        {
            
        }

        public void HandleMcpeRemoveObjective(McpeRemoveObjective message)
        {
            
        }

        public void HandleMcpeSetDisplayObjective(McpeSetDisplayObjective message)
        {
            
        }

        public void HandleMcpeSetScore(McpeSetScore message)
        {
            
        }

        public void HandleMcpeLabTable(McpeLabTable message)
        {
            
        }

        public void HandleMcpeUpdateBlockSynced(McpeUpdateBlockSynced message)
        {
            
        }

        public void HandleMcpeMoveEntityDelta(McpeMoveEntityDelta message)
        {
            
        }

        public void HandleMcpeSetScoreboardIdentityPacket(McpeSetScoreboardIdentityPacket message)
        {
            
        }

        public void HandleMcpeUpdateSoftEnumPacket(McpeUpdateSoftEnumPacket message)
        {
            
        }

        public void HandleMcpeNetworkStackLatencyPacket(McpeNetworkStackLatencyPacket message)
        {
            
        }

        public void HandleMcpeScriptCustomEventPacket(McpeScriptCustomEventPacket message)
        {
            
        }

        public void HandleMcpeSpawnParticleEffect(McpeSpawnParticleEffect message)
        {
            
        }

        public void HandleMcpeAvailableEntityIdentifiers(McpeAvailableEntityIdentifiers message)
        {
            
        }

        public void HandleMcpeLevelSoundEventV2(McpeLevelSoundEventV2 message)
        {
            
        }

        public void HandleMcpeNetworkChunkPublisherUpdate(McpeNetworkChunkPublisherUpdate message)
        {
            
        }

        public void HandleMcpeBiomeDefinitionList(McpeBiomeDefinitionList message)
        {
            
        }

        public void HandleMcpeLevelSoundEvent(McpeLevelSoundEvent message)
        {
            
        }

        public void HandleMcpeLevelEventGeneric(McpeLevelEventGeneric message)
        {
            
        }

        public void HandleMcpeLecternUpdate(McpeLecternUpdate message)
        {
            
        }

        public void HandleMcpeVideoStreamConnect(McpeVideoStreamConnect message)
        {
            
        }

        public void HandleMcpeClientCacheStatus(McpeClientCacheStatus message)
        {
            
        }

        public void HandleMcpeOnScreenTextureAnimation(McpeOnScreenTextureAnimation message)
        {
            
        }

        public void HandleMcpeMapCreateLockedCopy(McpeMapCreateLockedCopy message)
        {
            
        }

        public void HandleMcpeStructureTemplateDataExportRequest(McpeStructureTemplateDataExportRequest message)
        {
            
        }

        public void HandleMcpeStructureTemplateDataExportResponse(McpeStructureTemplateDataExportResponse message)
        {
            
        }

        public void HandleMcpeUpdateBlockProperties(McpeUpdateBlockProperties message)
        {
            
        }

        public void HandleMcpeClientCacheBlobStatus(McpeClientCacheBlobStatus message)
        {
            
        }

        public void HandleMcpeClientCacheMissResponse(McpeClientCacheMissResponse message)
        {
            
        }

        public void HandleFtlCreatePlayer(FtlCreatePlayer message)
        {
            
        }
    }
}