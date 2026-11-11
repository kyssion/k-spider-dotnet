-- Live E2E 取消任务夹具：每次执行插入一条新的 pushed 任务。
-- 任务包带 fixture=live-cancel，供 web/tests/live/real-backend.spec.ts 定位；
-- dispatch_token 每次不同，避免与历史任务或对账重派 token 冲突。
INSERT INTO sys_task (node_id, task_type, task_detail, status, extra)
SELECT
    '11111111-1111-1111-1111-111111111111'::uuid,
    'channel_round',
    jsonb_build_object(
        'channel_id', c.id,
        'round_type', 'list',
        'batch_limit', 1,
        'fixture', 'live-cancel'
    ),
    'pushed',
    jsonb_build_object(
        'fail_count', 0,
        'dispatch_token', 'live-cancel-' || gen_random_uuid()
    )
FROM channel c
WHERE c.channel_name = '测试渠道'
ORDER BY c.id
LIMIT 1;
