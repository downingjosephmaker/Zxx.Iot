<script setup lang="ts">
import { ref, computed, watch } from "vue";

defineOptions({
  name: "IotSchemaForm"
});

/** JSON Schema 字段(仅支持 object.properties 一层，覆盖动态表单常见形态) */
interface SchemaField {
  name: string;
  title: string;
  type: "string" | "number" | "integer" | "boolean" | "enum";
  enumValues?: Array<string | number>;
  required: boolean;
  defaultValue?: unknown;
  description?: string;
}

interface Props {
  /** JSON Schema 文本(object.properties 一层，与 ProductCommand.ParamSchema 同构) */
  schema: string;
  /** 表单值对象(父组件持有并读取；本组件按 schema 补种缺省值、清理游离键) */
  model: Record<string, unknown>;
  labelWidth?: string;
}

const props = withDefaults(defineProps<Props>(), {
  labelWidth: "110px"
});

const formRef = ref();

const fields = computed<SchemaField[]>(() => {
  if (!props.schema) return [];
  let schema: any;
  try {
    schema = JSON.parse(props.schema);
  } catch {
    return [];
  }
  const properties = schema?.properties;
  if (!properties || typeof properties !== "object") return [];
  const requiredList: string[] = Array.isArray(schema.required)
    ? schema.required
    : [];
  return Object.keys(properties).map(key => {
    const def = properties[key] ?? {};
    const hasEnum = Array.isArray(def.enum) && def.enum.length > 0;
    return {
      name: key,
      title: def.title || key,
      type: hasEnum ? "enum" : def.type || "string",
      enumValues: hasEnum ? def.enum : undefined,
      required: requiredList.includes(key),
      defaultValue: def.default,
      description: def.description
    } as SchemaField;
  });
});

/** 字段的类型缺省值 */
function fallbackValue(field: SchemaField): unknown {
  return (
    field.defaultValue ??
    (field.type === "boolean"
      ? false
      : field.type === "number" || field.type === "integer"
        ? 0
        : "")
  );
}

/** schema变化时清理游离键并补种缺省值；已有值按字段类型轻度纠偏(如配置里的"120"→120) */
watch(
  fields,
  list => {
    const names = new Set(list.map(f => f.name));
    Object.keys(props.model).forEach(key => {
      if (!names.has(key)) delete props.model[key];
    });
    list.forEach(field => {
      const val = props.model[field.name];
      if (val === undefined || val === null) {
        props.model[field.name] = fallbackValue(field);
      } else if (
        (field.type === "number" || field.type === "integer") &&
        typeof val !== "number"
      ) {
        const num = Number(val);
        props.model[field.name] = Number.isNaN(num) ? 0 : num;
      } else if (field.type === "boolean" && typeof val !== "boolean") {
        props.model[field.name] = val === "true" || val === "1" || val === 1;
      }
    });
  },
  { immediate: true }
);

const rules = computed(() => {
  const result: Record<string, unknown[]> = {};
  fields.value.forEach(field => {
    if (field.required) {
      result[field.name] = [
        {
          required: true,
          message: `${field.title}不能为空`,
          trigger:
            field.type === "enum" || field.type === "boolean"
              ? "change"
              : "blur"
        }
      ];
    }
  });
  return result;
});

async function validate(): Promise<boolean> {
  if (!fields.value.length || !formRef.value) return true;
  return formRef.value
    .validate()
    .then(() => true)
    .catch(() => false);
}

defineExpose({ validate });
</script>

<template>
  <el-form
    v-if="fields.length"
    ref="formRef"
    :model="model"
    :rules="rules"
    :label-width="labelWidth"
  >
    <el-form-item
      v-for="field in fields"
      :key="field.name"
      :label="field.title"
      :prop="field.name"
    >
      <el-select
        v-if="field.type === 'enum'"
        v-model="model[field.name]"
        :placeholder="field.description || '请选择'"
        class="w-full"
      >
        <el-option
          v-for="opt in field.enumValues"
          :key="String(opt)"
          :label="String(opt)"
          :value="opt"
        />
      </el-select>
      <el-switch
        v-else-if="field.type === 'boolean'"
        v-model="model[field.name]"
      />
      <el-input-number
        v-else-if="field.type === 'number' || field.type === 'integer'"
        v-model="model[field.name]"
        :step="1"
        controls-position="right"
      />
      <el-input
        v-else
        v-model="model[field.name]"
        :placeholder="field.description || '请输入'"
        clearable
      />
    </el-form-item>
  </el-form>
  <slot v-else name="empty">
    <el-text type="info" size="small">无可配置参数。</el-text>
  </slot>
</template>
