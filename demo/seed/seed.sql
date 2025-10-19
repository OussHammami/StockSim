-- demo seed: inserts a demo portfolio, positions and a couple of sample orders for user 'demo-user'
-- NOTE: This assumes EF migrations have already created the schema. If you changed the schema, update accordingly.

DO $$
BEGIN
  -- upsert portfolio for demo-user
  INSERT INTO "Portfolios" ("UserId", "Cash")
  VALUES ('demo-user', 100000.00)
  ON CONFLICT ("UserId") DO UPDATE SET "Cash" = EXCLUDED."Cash";

  -- ensure a couple of positions
  INSERT INTO "Positions" ("UserId", "Symbol", "Quantity", "AvgPrice")
  VALUES
    ('demo-user', 'AAPL', 10, 150.00),
    ('demo-user', 'MSFT', 5, 320.00)
  ON CONFLICT ("UserId","Symbol") DO UPDATE SET "Quantity" = EXCLUDED."Quantity", "AvgPrice" = EXCLUDED."AvgPrice";

  -- insert a sample pending order (limit buy) for demo
  INSERT INTO "Orders" ("OrderId","UserId","Symbol","Quantity","Remaining","Status","Type","Tif","SubmittedUtc","LimitPrice")
  VALUES
    (gen_random_uuid(), 'demo-user', 'AAPL', 10, 10, 1, 1, 0, now(), 145.00)
  ON CONFLICT DO NOTHING;
END $$;