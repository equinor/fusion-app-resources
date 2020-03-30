import FusionLogo from "./FusionLogo";
import * as styles from "./styles.less";
import * as React from "react";

const FusionIcon:React.FC= () => {
    return (
        <div className={styles.fusionIconContainer}>
            <FusionLogo scale={0.7}/>
        </div>
    )
}
export default FusionIcon;