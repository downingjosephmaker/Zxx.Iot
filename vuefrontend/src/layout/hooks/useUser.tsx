import { addDialog } from "@/components/ReDialog";
import { message } from "@/utils/message";
import { ElForm, ElFormItem, ElInput, ElProgress } from "element-plus";
import { reactive, ref, watch } from "vue";
import { isAllEmpty } from "@pureadmin/utils";
import { zxcvbn } from "@zxcvbn-ts/core";
import { editUserPwd } from "@/api/system";
import md5 from "md5";
import JSEncrypt from "jsencrypt";
export function useUser() {
  const ruleFormRef = ref();
  const PUBLIC_KEY =
    "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDR79EzLf9vlig9TIxCEnpOKgAoed6f0l+JDQO03eSi0rybQeUh/Oi0TF1wySvqEUhY/HbBFnZr2/JqrPTnlNfCOiTlGI47BXsndN1E5AG/wj8MUkZ5p3PMMrvqN+Zyf1MYDc7K4Eub0oGLwUgM9LHXVltKPLzazPZS5EkszYBetQIDAQAB";
  // 重置的新密码
  const pwdForm = reactive({
    newpwd: "",
    oldpwd: "",
    confirmpwd: ""
  });
  const pwdProgress = [
    { color: "#e74242", text: "非常弱" },
    { color: "#EFBD47", text: "弱" },
    { color: "#ffa500", text: "一般" },
    { color: "#1bbf1b", text: "强" },
    { color: "#008000", text: "非常强" }
  ];
  const curScore = ref();
  watch(pwdForm, ({ newpwd }) => {
    return (curScore.value = isAllEmpty(newpwd) ? -1 : zxcvbn(newpwd).score);
  });

  // 混合加密函数
  const hybridEncrypt = (password: string) => {
    const md5Hash = md5(password); // 生成MD5哈希
    console.log("🚀 ~ hybridEncrypt ~ md5Hash:", md5Hash);
    const encryptor = new JSEncrypt();
    encryptor.setPublicKey(PUBLIC_KEY);
    return encryptor.encrypt(md5Hash) || ""; // 对哈希值进行RSA加密
  };
  function updatePassword() {
    addDialog({
      title: `修改密码`,
      width: "30%",
      draggable: true,
      closeOnClickModal: false,
      contentRenderer: () => (
        <>
          <ElForm ref={ruleFormRef} model={pwdForm}>
            <ElFormItem
              prop="oldpwd"
              rules={[
                {
                  required: true,
                  message: "请输入旧密码",
                  trigger: "blur"
                }
              ]}
            >
              <ElInput
                clearable
                show-password
                type="password"
                v-model={pwdForm.oldpwd}
                placeholder="请输入旧密码"
              />
            </ElFormItem>
            <ElFormItem
              prop="newpwd"
              rules={[
                {
                  required: true,
                  message: "请输入新密码",
                  trigger: "blur"
                }
              ]}
            >
              <ElInput
                clearable
                show-password
                type="password"
                v-model={pwdForm.newpwd}
                placeholder="请输入新密码"
              />
            </ElFormItem>
            <div class="mt-4 flex">
              {pwdProgress.map(({ color, text }, idx) => (
                <div
                  class="w-[19vw]"
                  style={{ marginLeft: idx !== 0 ? "4px" : 0 }}
                >
                  <ElProgress
                    striped
                    striped-flow
                    duration={curScore.value === idx ? 6 : 0}
                    percentage={curScore.value >= idx ? 100 : 0}
                    color={color}
                    stroke-width={10}
                    show-text={false}
                  />
                  <p
                    class="text-center"
                    style={{ color: curScore.value === idx ? color : "" }}
                  >
                    {text}
                  </p>
                </div>
              ))}
            </div>
            <ElFormItem
              prop="confirmpwd"
              rules={[
                {
                  required: true,
                  message: "请确认密码",
                  trigger: "blur"
                },
                {
                  validator: (rule, value, callback) => {
                    if (value != pwdForm.newpwd) {
                      callback(new Error("新密码与确认密码不同"));
                    } else {
                      callback();
                    }
                  },
                  trigger: "blur"
                  // trigger: "click" // 如果想在点击确定按钮时触发这个校验，trigger 设置成 click 即可
                }
              ]}
            >
              <ElInput
                clearable
                show-password
                type="password"
                v-model={pwdForm.confirmpwd}
                placeholder="请确认密码"
              />
            </ElFormItem>
          </ElForm>
        </>
      ),
      closeCallBack: () => (pwdForm.newpwd = ""),
      beforeSure: done => {
        ruleFormRef.value.validate(async valid => {
          if (valid) {
            // 表单规则校验通过
            const parms = {
              oldpwd: hybridEncrypt(pwdForm.oldpwd),
              newpwd: hybridEncrypt(pwdForm.newpwd)
            };
            const data = await editUserPwd(parms);
            if (data.Status) {
              message(`已成功修改密码`, {
                type: "success"
              });
              // 根据实际业务使用pwdForm.newPwd和row里的某些字段去调用重置用户密码接口即可
              done();
            } else {
              message(data.Message, {
                type: "error"
              });
            }
          }
        });
      }
    });
  }
  return {
    updatePassword
  };
}
