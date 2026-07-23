using UnityEngine;
using UnityEngine.UI;

public class NoAdsBox : BaseBox<NoAdsBox>
{
    public Button btnClose;
    public Button btnCloseByPanel;

    public Button btnPurchase;

    protected override void Init()
    {
        btnClose.OnClicked(Close);        
        btnCloseByPanel.OnClicked(Close);
        
        btnPurchase.OnClicked(delegate
        {
            
        });
    }

    protected override void InitState()
    {
        
    }
}