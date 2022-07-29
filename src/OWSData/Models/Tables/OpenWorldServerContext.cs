using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OWSData.Models.StoredProcs;

namespace OWSData.Models.Tables
{
    public partial class OpenWorldServerContext : DbContext
    {
        public OpenWorldServerContext()
        {
        }

        public OpenWorldServerContext(DbContextOptions<OpenWorldServerContext> options)
            : base(options)
        {
        }
        /*
        public virtual DbSet<Abilities> Abilities { get; set; }
        public virtual DbSet<AbilityTypes> AbilityTypes { get; set; }
        public virtual DbSet<AreaOfInterestTypes> AreaOfInterestTypes { get; set; }
        public virtual DbSet<AreasOfInterest> AreasOfInterest { get; set; }
        public virtual DbSet<CharAbilityBarAbilities> CharAbilityBarAbilities { get; set; }
        public virtual DbSet<CharAbilityBars> CharAbilityBars { get; set; }
        public virtual DbSet<Characters> Characters { get; set; }
        public virtual DbSet<CharHasAbilities> CharHasAbilities { get; set; }
        public virtual DbSet<CharHasItems> CharHasItems { get; set; }
        public virtual DbSet<CharInventory> CharInventory { get; set; }
        public virtual DbSet<CharInventoryItems> CharInventoryItems { get; set; }
        public virtual DbSet<CharOnMapInstance> CharOnMapInstance { get; set; }
        public virtual DbSet<ChatGroups> ChatGroups { get; set; }
        public virtual DbSet<ChatGroupUsers> ChatGroupUsers { get; set; }
        public virtual DbSet<ChatMessages> ChatMessages { get; set; }
        public virtual DbSet<Class> Class { get; set; }
        public virtual DbSet<CustomCharacterData> CustomCharacterData { get; set; }
        public virtual DbSet<Customers> Customers { get; set; }
        public virtual DbSet<DebugLog> DebugLog { get; set; }
        public virtual DbSet<GlobalData> GlobalData { get; set; }
        public virtual DbSet<Items> Items { get; set; }
        public virtual DbSet<ItemTypes> ItemTypes { get; set; }
        public virtual DbSet<MapInstances> MapInstances { get; set; }
        public virtual DbSet<Maps> Maps { get; set; }
        public virtual DbSet<PlayerGroup> PlayerGroup { get; set; }
        public virtual DbSet<PlayerGroupCharacters> PlayerGroupCharacters { get; set; }
        public virtual DbSet<PlayerGroupTypes> PlayerGroupTypes { get; set; }
        public virtual DbSet<Races> Races { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserSessions> UserSessions { get; set; }
        public virtual DbSet<UsersInQueue> UsersInQueue { get; set; }
        public virtual DbSet<WorldServers> WorldServers { get; set; }

        //Stored Procs
        public DbSet<GetCharByCharName> GetCharByCharName { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=desktop-rvdfpao\\sqlexpress;Database=OpenWorldServer;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Abilities>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.AbilityId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.AbilityId)
                    .HasColumnName("AbilityID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AbilityCustomJson)
                    .HasColumnName("AbilityCustomJSON")
                    .IsUnicode(false);

                entity.Property(e => e.AbilityName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.AbilityTypeId).HasColumnName("AbilityTypeID");

                entity.Property(e => e.GameplayAbilityClassName)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.TextureToUseForIcon)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

            });

            modelBuilder.Entity<AbilityTypes>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.AbilityTypeId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.AbilityTypeId)
                    .HasColumnName("AbilityTypeID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AbilityTypeName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<AreaOfInterestTypes>(entity =>
            {
                entity.HasKey(e => e.AreaOfInterestTypeId);

                entity.Property(e => e.AreaOfInterestTypeId).HasColumnName("AreaOfInterestTypeID");

                entity.Property(e => e.AreaOfInterestTypeDesc)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<AreasOfInterest>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.AreasOfInterestGuid });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.AreasOfInterestGuid).HasColumnName("AreasOfInterestGUID");

                entity.Property(e => e.AreaOfInterestName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CustomData).IsUnicode(false);

                entity.Property(e => e.MapZoneId).HasColumnName("MapZoneID");

                entity.Property(e => e.Rx).HasColumnName("RX");

                entity.Property(e => e.Ry).HasColumnName("RY");

                entity.Property(e => e.Rz).HasColumnName("RZ");
            });

            modelBuilder.Entity<CharAbilityBarAbilities>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.CharAbilityBarAbilityId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CharAbilityBarAbilityId)
                    .HasColumnName("CharAbilityBarAbilityID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.CharAbilityBarAbilitiesCustomJson)
                    .HasColumnName("CharAbilityBarAbilitiesCustomJSON")
                    .IsUnicode(false);

                entity.Property(e => e.CharAbilityBarId).HasColumnName("CharAbilityBarID");

                entity.Property(e => e.CharHasAbilitiesId).HasColumnName("CharHasAbilitiesID");

                entity.Property(e => e.InSlotNumber).HasDefaultValueSql("((1))");

                entity.HasOne(d => d.C)
                    .WithMany(p => p.CharAbilityBarAbilities)
                    .HasForeignKey(d => new { d.CustomerGuid, d.CharAbilityBarId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CharAbilityBarAbilities_CharAbilityBarID");

                entity.HasOne(d => d.CNavigation)
                    .WithMany(p => p.CharAbilityBarAbilities)
                    .HasForeignKey(d => new { d.CustomerGuid, d.CharHasAbilitiesId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CharAbilityBarAbilities_CharHasAbilities");
            });

            modelBuilder.Entity<CharAbilityBars>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.CharAbilityBarId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CharAbilityBarId)
                    .HasColumnName("CharAbilityBarID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AbilityBarName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.CharAbilityBarsCustomJson)
                    .HasColumnName("CharAbilityBarsCustomJSON")
                    .IsUnicode(false);

                entity.Property(e => e.CharacterId).HasColumnName("CharacterID");

                entity.Property(e => e.MaxNumberOfSlots).HasDefaultValueSql("((1))");

                entity.Property(e => e.NumberOfUnlockedSlots).HasDefaultValueSql("((1))");
            });

            modelBuilder.Entity<Characters>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.CharacterId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CharacterId)
                    .HasColumnName("CharacterID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.BaseMesh)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.CharName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ClassId).HasColumnName("ClassID");

                entity.Property(e => e.DefaultPawnClassPath)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.Description).IsUnicode(false);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.LastActivity)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.MapName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Rx).HasColumnName("RX");

                entity.Property(e => e.Ry).HasColumnName("RY");

                entity.Property(e => e.Rz).HasColumnName("RZ");

                entity.Property(e => e.ServerIp)
                    .HasColumnName("ServerIP")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.UserGuid).HasColumnName("UserGUID");

                entity.Property(e => e.Weight).HasColumnName("weight");

                entity.Property(e => e.Xp).HasColumnName("XP");
            });

            modelBuilder.Entity<CharHasAbilities>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.CharHasAbilitiesId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CharHasAbilitiesId)
                    .HasColumnName("CharHasAbilitiesID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AbilityId).HasColumnName("AbilityID");

                entity.Property(e => e.AbilityLevel).HasDefaultValueSql("((1))");

                entity.Property(e => e.CharHasAbilitiesCustomJson)
                    .HasColumnName("CharHasAbilitiesCustomJSON")
                    .IsUnicode(false);

                entity.Property(e => e.CharacterId).HasColumnName("CharacterID");

                entity.HasOne(d => d.C)
                    .WithMany(p => p.CharHasAbilities)
                    .HasForeignKey(d => new { d.CustomerGuid, d.CharacterId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CharHasAbilities_CharacterID");
            });

            modelBuilder.Entity<CharHasItems>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.CharacterId, e.CharHasItemId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CharacterId).HasColumnName("CharacterID");

                entity.Property(e => e.CharHasItemId)
                    .HasColumnName("CharHasItemID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ItemId).HasColumnName("ItemID");

                entity.Property(e => e.Quantity).HasDefaultValueSql("((1))");

                entity.HasOne(d => d.C)
                    .WithMany(p => p.CharHasItems)
                    .HasForeignKey(d => new { d.CustomerGuid, d.CharacterId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CharHasItems_CharacterID");
            });

            modelBuilder.Entity<CharInventory>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.CharacterId, e.CharInventoryId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CharacterId).HasColumnName("CharacterID");

                entity.Property(e => e.CharInventoryId)
                    .HasColumnName("CharInventoryID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.InventoryName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<CharInventoryItems>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.CharInventoryId, e.CharInventoryItemId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CharInventoryId).HasColumnName("CharInventoryID");

                entity.Property(e => e.CharInventoryItemId)
                    .HasColumnName("CharInventoryItemID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.CharInventoryItemGuid)
                    .HasColumnName("CharInventoryItemGUID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.Condition).HasDefaultValueSql("((100))");

                entity.Property(e => e.ItemId).HasColumnName("ItemID");
            });

            modelBuilder.Entity<CharOnMapInstance>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.CharacterId, e.MapInstanceId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CharacterId).HasColumnName("CharacterID");

                entity.Property(e => e.MapInstanceId).HasColumnName("MapInstanceID");
            });

            modelBuilder.Entity<ChatGroups>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.ChatGroupId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.ChatGroupId)
                    .HasColumnName("ChatGroupID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ChatGroupName)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<ChatGroupUsers>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.ChatGroupId, e.CharacterId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.ChatGroupId).HasColumnName("ChatGroupID");

                entity.Property(e => e.CharacterId).HasColumnName("CharacterID");
            });

            modelBuilder.Entity<ChatMessages>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.ChatMessageId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.ChatMessageId)
                    .HasColumnName("ChatMessageID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ChatGroupId).HasColumnName("ChatGroupID");

                entity.Property(e => e.ChatMessage).IsRequired();

                entity.Property(e => e.MessageDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.SentByCharId).HasColumnName("SentByCharID");

                entity.Property(e => e.SentToCharId).HasColumnName("SentToCharID");
            });

            modelBuilder.Entity<Class>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.ClassId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.ClassId)
                    .HasColumnName("ClassID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ClassName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.Description).IsUnicode(false);

                entity.Property(e => e.Rx).HasColumnName("RX");

                entity.Property(e => e.Ry).HasColumnName("RY");

                entity.Property(e => e.Rz).HasColumnName("RZ");

                entity.Property(e => e.StartingMapName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Weight).HasColumnName("weight");

                entity.Property(e => e.Xp).HasColumnName("XP");
            });

            modelBuilder.Entity<CustomCharacterData>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.CustomCharacterDataId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CustomCharacterDataId)
                    .HasColumnName("CustomCharacterDataID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.CharacterId).HasColumnName("CharacterID");

                entity.Property(e => e.CustomFieldName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FieldValue)
                    .IsRequired()
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Customers>(entity =>
            {
                entity.HasKey(e => e.CustomerGuid);

                entity.Property(e => e.CustomerGuid)
                    .HasColumnName("CustomerGUID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.CustomerEmail)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.CustomerName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CustomerNotes).IsUnicode(false);

                entity.Property(e => e.CustomerPhone)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.EnableAutoLoopBack)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.FreeTrialStarted).HasColumnType("datetime");

                entity.Property(e => e.PublisherPaidDate).HasColumnType("datetime");

                entity.Property(e => e.StripeCustomerId)
                    .IsRequired()
                    .HasColumnName("StripeCustomerID")
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");
            });

            modelBuilder.Entity<DebugLog>(entity =>
            {
                entity.Property(e => e.DebugLogId).HasColumnName("DebugLogID");

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.DebugDate).HasColumnType("datetime");

                entity.Property(e => e.DebugDesc)
                    .IsRequired()
                    .IsUnicode(false);
            });

            modelBuilder.Entity<GlobalData>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.GlobalDataKey });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.GlobalDataKey)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.GlobalDataValue)
                    .IsRequired()
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Items>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.ItemId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.ItemId)
                    .HasColumnName("ItemID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.CanBeDropped)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.CustomData)
                    .IsRequired()
                    .HasMaxLength(2000)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.ItemCode)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.ItemDescription)
                    .IsRequired()
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.ItemIsConsumedOnUse)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ItemMesh)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.ItemName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ItemTypeId).HasColumnName("ItemTypeID");

                entity.Property(e => e.MeshToUseForPickup)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.SkeletalMesh)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.StaticMesh)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.TextureToUseForIcon)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.WeaponActorClass)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");
            });

            modelBuilder.Entity<ItemTypes>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.ItemTypeId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.ItemTypeId)
                    .HasColumnName("ItemTypeID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ItemTypeDesc)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<MapInstances>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.MapInstanceId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.MapInstanceId)
                    .HasColumnName("MapInstanceID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.LastServerEmptyDate).HasColumnType("datetime");

                entity.Property(e => e.LastUpdateFromServer).HasColumnType("datetime");

                entity.Property(e => e.MapId).HasColumnName("MapID");

                entity.Property(e => e.PlayerGroupId).HasColumnName("PlayerGroupID");

                entity.Property(e => e.Status).HasDefaultValueSql("((1))");

                entity.Property(e => e.WorldServerId).HasColumnName("WorldServerID");
            });

            modelBuilder.Entity<Maps>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.MapId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.MapId)
                    .HasColumnName("MapID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.HardPlayerCap).HasDefaultValueSql("((80))");

                entity.Property(e => e.MapMode).HasDefaultValueSql("((1))");

                entity.Property(e => e.MapName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.SoftPlayerCap).HasDefaultValueSql("((60))");

                entity.Property(e => e.WorldCompContainsFilter)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.WorldCompListFilter)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.ZoneName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<PlayerGroup>(entity =>
            {
                entity.HasKey(e => new { e.PlayerGroupId, e.CustomerGuid });

                entity.Property(e => e.PlayerGroupId)
                    .HasColumnName("PlayerGroupID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.PlayerGroupName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.PlayerGroupTypeId).HasColumnName("PlayerGroupTypeID");

                entity.HasOne(d => d.PlayerGroupType)
                    .WithMany(p => p.PlayerGroup)
                    .HasForeignKey(d => d.PlayerGroupTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PlayerGroup_PlayerGroupType");
            });

            modelBuilder.Entity<PlayerGroupCharacters>(entity =>
            {
                entity.HasKey(e => new { e.PlayerGroupId, e.CustomerGuid, e.CharacterId });

                entity.Property(e => e.PlayerGroupId).HasColumnName("PlayerGroupID");

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.CharacterId).HasColumnName("CharacterID");

                entity.Property(e => e.DateAdded)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<PlayerGroupTypes>(entity =>
            {
                entity.HasKey(e => e.PlayerGroupTypeId);

                entity.Property(e => e.PlayerGroupTypeId)
                    .HasColumnName("PlayerGroupTypeID")
                    .ValueGeneratedNever();

                entity.Property(e => e.PlayerGroupTypeDesc)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Races>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.RaceId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.RaceId)
                    .HasColumnName("RaceID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.RaceName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserGuid);

                entity.Property(e => e.UserGuid)
                    .HasColumnName("UserGUID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.LastAccess)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.Salt)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<UserSessions>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.UserSessionGuid });

                entity.HasIndex(e => e.UserGuid);

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.UserSessionGuid)
                    .HasColumnName("UserSessionGUID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.LoginDate).HasColumnType("datetime");

                entity.Property(e => e.SelectedCharacterName).HasMaxLength(50);

                entity.Property(e => e.UserGuid).HasColumnName("UserGUID");

                entity.HasOne(d => d.UserGu)
                    .WithMany(p => p.UserSessions)
                    .HasForeignKey(d => d.UserGuid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserSessions_UserGUID");
            });

            modelBuilder.Entity<UsersInQueue>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.UserGuid, e.QueueName });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.UserGuid).HasColumnName("UserGUID");

                entity.Property(e => e.QueueName)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.JoinDt)
                    .HasColumnName("JoinDT")
                    .HasColumnType("datetime");
            });

            modelBuilder.Entity<WorldServers>(entity =>
            {
                entity.HasKey(e => new { e.CustomerGuid, e.WorldServerId });

                entity.Property(e => e.CustomerGuid).HasColumnName("CustomerGUID");

                entity.Property(e => e.WorldServerId)
                    .HasColumnName("WorldServerID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ActiveStartTime).HasColumnType("datetime");

                entity.Property(e => e.InternalServerIp)
                    .IsRequired()
                    .HasColumnName("InternalServerIP")
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.Port).HasDefaultValueSql("((8181))");

                entity.Property(e => e.ServerIp)
                    .IsRequired()
                    .HasColumnName("ServerIP")
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });
        }*/
    }
}
