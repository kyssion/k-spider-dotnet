-- ============================================================
-- k-spider 认证表物理结构基线 : sys_user
-- 认证行为 / 接口 / 配置唯一权威见 docs/auth.md
-- 本文件只定义物理结构 / 约束 / 索引 / 种子
-- 幂等脚本 : 全部 IF NOT EXISTS，可重复执行
-- ============================================================

-- ============================================================
-- sys_user 账号表
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_user (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username      text NOT NULL
        CHECK (char_length(username) BETWEEN 3 AND 32
            AND username ~ '^[a-zA-Z0-9._-]+$'),
    password_hash text NOT NULL,
    display_name  text,
    status        text NOT NULL DEFAULT 'active'
        CHECK (status IN ('active', 'disabled')),
    create_time   timestamptz NOT NULL DEFAULT now(),
    update_time   timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE sys_user IS '账号表';
COMMENT ON COLUMN sys_user.id IS '代理主键';
COMMENT ON COLUMN sys_user.username IS '登录名（字符约束见 CHECK；大小写不敏感唯一）';
COMMENT ON COLUMN sys_user.password_hash IS '密码摘要（算法见 docs/auth.md）';
COMMENT ON COLUMN sys_user.display_name IS '页面展示名，可空';
COMMENT ON COLUMN sys_user.status IS '启停（取值见 CHECK；行为见 docs/auth.md）';
COMMENT ON COLUMN sys_user.create_time IS '创建时间';
COMMENT ON COLUMN sys_user.update_time IS '修改时间（应用层更新）';

CREATE UNIQUE INDEX IF NOT EXISTS sys_user_username_uk
    ON sys_user (lower(username));

-- 默认账号种子（要求见 docs/auth.md）
INSERT INTO sys_user (username, password_hash, display_name)
SELECT 'admin',
       'AQAAAAIAAYagAAAAEDCNmp4TQnHTyFkvht79HbH5Jb56fDLA6D4O09KukVl5UZQJIoEtZzNKBUdaDa8Rqw==',
       '管理员'
WHERE NOT EXISTS (SELECT 1 FROM sys_user)
ON CONFLICT DO NOTHING;
