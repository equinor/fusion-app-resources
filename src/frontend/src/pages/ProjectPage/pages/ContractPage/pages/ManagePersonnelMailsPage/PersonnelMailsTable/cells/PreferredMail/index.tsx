import { PersonPhoto, TextInput } from "@equinor/fusion-components";
import { FC } from "react";
import * as styles from "./styles.less";

const PreferredMail:FC = () =>{
    return <div className={styles.container}>
        <input className={styles.mailInput} value={""} onChange={() => {}}/>
    </div>
}

export default PreferredMail