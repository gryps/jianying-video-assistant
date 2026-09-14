"""add product categories

Revision ID: p18a1c3e5g70
Revises: o07h9i1j4k58
"""

from alembic import op
import sqlalchemy as sa


revision = "p18a1c3e5g70"
down_revision = "o07h9i1j4k58"
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.create_table(
        "wb_product_categories",
        sa.Column("id", sa.String(length=32), nullable=False),
        sa.Column("name", sa.String(length=80), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("updated_at", sa.DateTime(timezone=True), nullable=False),
        sa.PrimaryKeyConstraint("id"),
        sa.UniqueConstraint("name", name="uq_wb_product_categories_name"),
    )
    with op.batch_alter_table("wb_products") as batch_op:
        batch_op.add_column(sa.Column("category_id", sa.String(length=32), nullable=True))
        batch_op.create_index("ix_wb_products_category_id", ["category_id"], unique=False)
        batch_op.create_foreign_key(
            "fk_wb_products_category_id",
            "wb_product_categories",
            ["category_id"],
            ["id"],
            ondelete="SET NULL",
        )


def downgrade() -> None:
    with op.batch_alter_table("wb_products") as batch_op:
        batch_op.drop_constraint("fk_wb_products_category_id", type_="foreignkey")
        batch_op.drop_index("ix_wb_products_category_id")
        batch_op.drop_column("category_id")
    op.drop_table("wb_product_categories")
