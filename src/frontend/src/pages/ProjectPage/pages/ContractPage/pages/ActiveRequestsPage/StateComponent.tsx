import * as React from "react";
import PersonnelRequest from '../../../../../../models/PersonnelRequest';

type StateComponentProps = {
    item: PersonnelRequest
}

const StateComponent: React.FC<StateComponentProps> = ({ item }) => {
    return <div>
        {item.state}
    </div>
}
export default StateComponent