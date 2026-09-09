-- BigQuery Standard SQL. Replace PROJECT_ID and ANALYTICS_DATASET before use.
-- Deferred by product decision (2026-09-09). Not a launch requirement.
-- Read-only baseline queries; not executed against a live dataset.
-- Use finalized daily export tables only, avoiding overlap with intraday exports.
WITH events AS (
  SELECT
    TIMESTAMP_MICROS(event_timestamp) AS occurred_at,
    DATE(TIMESTAMP_MICROS(event_timestamp), 'UTC') AS day,
    user_pseudo_id,
    platform,
    geo.country AS audience_country,
    event_name,
    (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'event_id') AS event_id,
    (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'format') AS format,
    (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'offer_id') AS offer_id,
    (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'reward_id') AS reward_id,
    (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'status') AS status,
    (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'currency') AS currency,
    (SELECT COALESCE(value.double_value, CAST(value.int_value AS FLOAT64)) FROM UNNEST(event_params) WHERE key = 'value') AS revenue,
    (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'environment') AS environment
  FROM `PROJECT_ID.ANALYTICS_DATASET.events_*`
  WHERE REGEXP_CONTAINS(_TABLE_SUFFIX, r'^\d{8}$')
    AND _TABLE_SUFFIX BETWEEN FORMAT_DATE('%Y%m%d', DATE_SUB(CURRENT_DATE('UTC'), INTERVAL 30 DAY))
      AND FORMAT_DATE('%Y%m%d', CURRENT_DATE('UTC'))
), deduplicated AS (
  -- Custom events have event_id. Automatic events are preserved; do not infer
  -- duplicate impressions from equal timestamps or identical revenue values.
  SELECT * FROM events WHERE event_id IS NULL
  UNION ALL
  SELECT * FROM events WHERE event_id IS NOT NULL
  QUALIFY ROW_NUMBER() OVER (PARTITION BY user_pseudo_id, event_id ORDER BY occurred_at) = 1
), active AS (
  SELECT day, platform, audience_country, COUNT(DISTINCT user_pseudo_id) AS active_players
  FROM deduplicated
  WHERE event_name = 'puzzle_started' AND environment = 'production'
  GROUP BY 1, 2, 3
), canonical_ads AS (
  -- Use a dedicated production Firebase project. SDK automatic events do not
  -- inherit our custom environment parameter.
  SELECT day, platform, audience_country, currency,
    COUNT(*) AS impressions,
    COUNTIF(revenue IS NULL OR currency IS NULL) AS impressions_without_value,
    SUM(IF(currency IS NOT NULL, revenue, NULL)) AS ad_revenue
  FROM deduplicated
  WHERE event_name = 'ad_impression'
  GROUP BY 1, 2, 3, 4
)
SELECT a.*, d.active_players,
  SAFE_DIVIDE(a.ad_revenue, d.active_players) AS ad_arpdau,
  SAFE_DIVIDE(a.ad_revenue * 1000, a.impressions) AS ecpm,
  SAFE_DIVIDE(a.impressions, d.active_players) AS impressions_per_active
FROM canonical_ads a
LEFT JOIN active d USING (day, platform, audience_country)
ORDER BY day DESC, platform, currency;

-- Offer/reward integrity. Deduplicate logical identifiers, not callback counts.
WITH events AS (
  SELECT event_name, user_pseudo_id,
    (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'offer_id') AS offer_id,
    (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'reward_id') AS reward_id,
    (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'source') AS source
  FROM `PROJECT_ID.ANALYTICS_DATASET.events_*`
  WHERE REGEXP_CONTAINS(_TABLE_SUFFIX, r'^\d{8}$')
    AND _TABLE_SUFFIX BETWEEN FORMAT_DATE('%Y%m%d', DATE_SUB(CURRENT_DATE('UTC'), INTERVAL 30 DAY))
      AND FORMAT_DATE('%Y%m%d', CURRENT_DATE('UTC'))
), counts AS (
  SELECT
    COUNT(DISTINCT IF(event_name = 'reward_offer_view', CONCAT(user_pseudo_id, ':', offer_id), NULL)) AS offers,
    COUNT(DISTINCT IF(event_name = 'reward_offer_accept', CONCAT(user_pseudo_id, ':', offer_id), NULL)) AS accepted,
    COUNT(DISTINCT IF(event_name = 'reward_earned', CONCAT(user_pseudo_id, ':', NULLIF(reward_id, '')), NULL)) AS earned,
    COUNT(DISTINCT IF(event_name = 'hint_granted' AND source = 'reward', CONCAT(user_pseudo_id, ':', NULLIF(reward_id, '')), NULL)) AS delivered
  FROM events
)
SELECT *, SAFE_DIVIDE(accepted, offers) AS acceptance,
  SAFE_DIVIDE(delivered, earned) AS reward_delivery FROM counts;
