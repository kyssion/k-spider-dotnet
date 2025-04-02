using Microsoft.EntityFrameworkCore;

namespace k_spirder_script.models;

public partial class KScriptSpiderContext : DbContext
{
    public KScriptSpiderContext()
    {
    }

    public KScriptSpiderContext(DbContextOptions<KScriptSpiderContext> options)
        : base(options)
    {
    }

    public virtual DbSet<SpiderNewsContent> SpiderNewsContents { get; set; }

    public virtual DbSet<SpiderNewsContentOrigin> SpiderNewsContentOrigins { get; set; }

    public virtual DbSet<SpiderNewsImageList> SpiderNewsImageLists { get; set; }

    public virtual DbSet<SpiderNewsList> SpiderNewsLists { get; set; }

    public virtual DbSet<StockCnIntroduction> StockCnIntroductions { get; set; }

    public virtual DbSet<StockCnLevel1ArchivedDailyOrigin> StockCnLevel1ArchivedDailyOrigins { get; set; }

    public virtual DbSet<StockHkLevel1ArchivedDailyOrigin> StockHkLevel1ArchivedDailyOrigins { get; set; }

    public virtual DbSet<StockUsaLevel1ArchivedDaliyOrigin> StockUsaLevel1ArchivedDaliyOrigins { get; set; }

//     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
// #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//         => optionsBuilder.UseNpgsql("PORT=5432;DATABASE=k_script_spider;HOST=39.100.86.193;PASSWORD=Javarustc++11.;USER ID=spider ;Include Error Detail=true;Pooling=true;MaxPoolSize=10");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SpiderNewsContent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("spider_news_content_pkey");

            entity.ToTable("spider_news_content", tb => tb.HasComment("新闻信息详情表"));

            entity.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<SpiderNewsContentOrigin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("spider_news_content_origin_pkey");

            entity.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.NewsOriginType).HasDefaultValue(0);
            entity.Property(e => e.Status).HasDefaultValue(0);
            entity.Property(e => e.UpdateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<SpiderNewsImageList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("spider_news_image_list_pkey");

            entity.ToTable("spider_news_image_list", tb => tb.HasComment("爬虫详情中的图片信息记录"));

            entity.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<SpiderNewsList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("spider_news_list_pkey");

            entity.ToTable("spider_news_list", tb => tb.HasComment("排重抓取信息信息列表"));

            entity.Property(e => e.Category)
                .HasDefaultValue(0)
                .HasComment("新闻类型");
            entity.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DownloadStatusCode)
                .HasDefaultValue(0)
                .HasComment("详情数据是否下载 0 没有下载 1 已下载");
            entity.Property(e => e.UpdateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<StockCnIntroduction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_cn_introduction_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('stock_introduction_id_seq'::regclass)");
            entity.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<StockCnLevel1ArchivedDailyOrigin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_cn_level1_archived_daliy_pkey");

            entity.ToTable("stock_cn_level1_archived_daily_origin", tb => tb.HasComment("中国股市信息天级别表归档"));

            entity.HasIndex(e => new { e.StockId, e.Date }, "stock_cn_id_date")
                .IsUnique()
                .HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('stock_cn_level1_archived_daliy_id_seq'::regclass)");
            entity.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<StockHkLevel1ArchivedDailyOrigin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_hk_level1_archived_daliy_pkey");

            entity.ToTable("stock_hk_level1_archived_daily_origin", tb => tb.HasComment("香港股市信息天级别level1原始数据"));

            entity.HasIndex(e => new { e.StockId, e.Date }, "stock_hk_id_date")
                .IsUnique()
                .HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('stock_hk_level1_archived_daliy_id_seq'::regclass)");
            entity.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<StockUsaLevel1ArchivedDaliyOrigin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_usa_level1_archived_daliy_pkey");

            entity.ToTable("stock_usa_level1_archived_daliy_origin", tb => tb.HasComment("美股股市信息天级别level1归档原始数据"));

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('stock_usa_level1_archived_daliy_id_seq'::regclass)");
            entity.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
